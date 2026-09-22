// ============================================================================
// booking_providers.dart — all 8 booking Riverpod providers.
//
// Each provider calls the real ASP.NET Core backend via BookingApiService.
// If the API is unreachable (DioException / connection refused) the provider
// falls back to the local seed data so the app stays usable during offline
// development / demo.
//
// Endpoints wired:
//   1. scheduleListProvider          → GET  /api/booking-schedules
//   2. createBookingProvider         → POST /api/bookings
//   3. travelerBookingsProvider      → GET  /api/bookings/traveler/{id}
//   4. bookingDetailProvider(id)     → GET  /api/bookings/{id}     (derived)
//   5. updateBookingStatusProvider   → PUT  /api/bookings/{id}/status
//   6. cancelBookingProvider         → DELETE /api/bookings/{id}
//   7. createDiscountRequestProvider → POST /api/discount-requests
//   8. processEscrowPaymentProvider  → POST /api/bookings/{id}/process-escrow-payment
// ============================================================================

import 'package:dio/dio.dart';
import 'package:flutter/foundation.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../models/booking.dart';
import '../services/booking_api_service.dart';

// ── Helpers: JSON → model mappers ───────────────────────────────────────────

BookingSchedule _scheduleFromJson(Map<String, dynamic> j) {
  return BookingSchedule(
    id: j['id'].toString(),
    destinationId: j['destinationId'].toString(),
    destinationTitle: j['destinationTitle'] as String,
    location: j['location'] as String,
    guideName: j['guideName'] as String,
    pricePerPerson: (j['pricePerPerson'] as num).toDouble(),
    maxCapacityPerSlot: j['maxCapacityPerSlot'] as int,
    rating: (j['rating'] as num).toDouble(),
    reviewsCount: j['reviewsCount'] as int,
    availableDates: (j['availableDates'] as List)
        .map((d) => DateTime.parse(d.toString()))
        .toList(),
    availableTimeSlots: List<String>.from(j['availableTimeSlots'] as List),
    bookedSlotsMap: (j['bookedSlotsMap'] as Map<String, dynamic>).map(
      (k, v) => MapEntry(k, (v as num).toInt()),
    ),
  );
}

BookingStatus _parseBookingStatus(String s) {
  switch (s.toLowerCase()) {
    case 'confirmed':
      return BookingStatus.confirmed;
    case 'completed':
      return BookingStatus.completed;
    case 'cancelled':
      return BookingStatus.cancelled;
    default:
      return BookingStatus.pending;
  }
}

EscrowStatus _parseEscrowStatus(String s) {
  switch (s.toLowerCase()) {
    case 'heldinescrow':
      return EscrowStatus.heldInEscrow;
    case 'released':
      return EscrowStatus.released;
    case 'refunded':
      return EscrowStatus.refunded;
    default:
      return EscrowStatus.pending;
  }
}

BookingPaymentMethod _parsePaymentMethod(String? s) {
  switch (s?.toLowerCase()) {
    case 'banktransferreceipt':
      return BookingPaymentMethod.bankTransferReceipt;
    case 'atmcashreceipt':
      return BookingPaymentMethod.atmCashReceipt;
    default:
      return BookingPaymentMethod.sampleCard;
  }
}

Booking _bookingFromJson(Map<String, dynamic> j) {
  return Booking(
    id: j['id'].toString(),
    scheduleId: j['scheduleId'].toString(),
    destinationTitle: j['destinationTitle'] as String,
    location: j['location'] as String,
    travelerId: j['travelerId'].toString(),
    travelerName: j['travelerName'] as String,
    travelerEmail: j['travelerEmail'] as String,
    date: DateTime.parse(j['bookingDate'] as String),
    timeSlot: j['timeSlot'] as String,
    guests: j['guests'] as int,
    basePrice: (j['basePrice'] as num).toDouble(),
    serviceFee: (j['serviceFee'] as num).toDouble(),
    discountAmount: (j['discountAmount'] as num).toDouble(),
    totalAmount: (j['totalAmount'] as num).toDouble(),
    status: _parseBookingStatus(j['status'] as String),
    paymentStatus: _parseEscrowStatus(j['paymentStatus'] as String),
    paymentMethod: _parsePaymentMethod(j['paymentMethod'] as String?),
    createdAt: DateTime.parse(j['createdAt'] as String),
    notes: j['notes'] as String?,
    transactionRef: j['transactionRef'] as String?,
    receiptReference: j['receiptReference'] as String?,
    receiptImageData: j['receiptImageData'] as String?,
    escrowReleaseDate: j['escrowReleaseDate'] != null
        ? DateTime.parse(j['escrowReleaseDate'] as String)
        : null,
  );
}

DiscountStatus _parseDiscountStatus(String s) {
  switch (s.toLowerCase()) {
    case 'approved':
      return DiscountStatus.approved;
    case 'rejected':
      return DiscountStatus.rejected;
    default:
      return DiscountStatus.pending;
  }
}

DiscountRequest _discountRequestFromJson(Map<String, dynamic> json) {
  return DiscountRequest(
    id: json['id'].toString(),
    bookingId: json['bookingId'].toString(),
    scheduleId: json['scheduleId'].toString(),
    travelerId: json['travelerId'].toString(),
    originalPrice: (json['originalPrice'] as num).toDouble(),
    requestedDiscountPercent: (json['requestedDiscountPercent'] as num)
        .toDouble(),
    reason: json['reason'] as String,
    status: _parseDiscountStatus(json['status'] as String),
    createdAt: DateTime.parse(json['createdAt'] as String),
  );
}

// ── Current logged-in traveler ───────────────────────────────────────────────
// NOTE: When the auth team wires up login, replace this with a real provider
// that reads the JWT claims. For now we use a well-known dev traveler ID.
final currentTravelerProvider = Provider<Map<String, String>>((ref) {
  return const {
    'id': '00000000-0000-0000-0000-000000000001',
    'name': 'Nithu Traveler',
    'email': 'traveler@travyle.com',
  };
});

// ── Seed data (offline / demo fallback) ─────────────────────────────────────
List<BookingSchedule> _seedSchedules() {
  final now = DateTime.now();
  return [
    BookingSchedule(
      id: '00000000-0000-0000-0001-000000000001',
      destinationId: '00000000-0000-0000-0002-000000000001',
      destinationTitle: 'Ella Rock & Nine Arch Bridge Trek',
      location: 'Ella, Badulla District',
      guideName: 'Kasun Bandara',
      pricePerPerson: 4500.0,
      availableDates: [
        now.add(const Duration(days: 1)),
        now.add(const Duration(days: 2)),
        now.add(const Duration(days: 3)),
        now.add(const Duration(days: 4)),
      ],
      availableTimeSlots: const [
        '06:30 AM',
        '09:00 AM',
        '02:00 PM',
        '04:30 PM',
      ],
      maxCapacityPerSlot: 8,
      bookedSlotsMap: {
        BookingSchedule.slotKey(now.add(const Duration(days: 1)), '06:30 AM'):
            8,
        BookingSchedule.slotKey(now.add(const Duration(days: 1)), '09:00 AM'):
            6,
        BookingSchedule.slotKey(now.add(const Duration(days: 2)), '02:00 PM'):
            3,
      },
      rating: 4.9,
      reviewsCount: 142,
    ),
    BookingSchedule(
      id: '00000000-0000-0000-0001-000000000002',
      destinationId: '00000000-0000-0000-0002-000000000002',
      destinationTitle: 'Sigiriya Ancient Rock Fortress Sunrise Tour',
      location: 'Sigiriya, Matale District',
      guideName: 'Anura Senanayake',
      pricePerPerson: 6000.0,
      availableDates: [
        now.add(const Duration(days: 1)),
        now.add(const Duration(days: 2)),
        now.add(const Duration(days: 5)),
      ],
      availableTimeSlots: const ['05:30 AM', '08:00 AM', '03:30 PM'],
      maxCapacityPerSlot: 10,
      bookedSlotsMap: {
        BookingSchedule.slotKey(now.add(const Duration(days: 1)), '05:30 AM'):
            5,
        BookingSchedule.slotKey(now.add(const Duration(days: 2)), '08:00 AM'):
            10,
      },
      rating: 4.8,
      reviewsCount: 98,
    ),
    BookingSchedule(
      id: '00000000-0000-0000-0001-000000000003',
      destinationId: '00000000-0000-0000-0002-000000000003',
      destinationTitle: 'Mirissa Blue Whale Watching Expedition',
      location: 'Mirissa Harbour, Southern Province',
      guideName: 'Chaminda Silva',
      pricePerPerson: 8500.0,
      availableDates: [
        now.add(const Duration(days: 2)),
        now.add(const Duration(days: 3)),
        now.add(const Duration(days: 6)),
      ],
      availableTimeSlots: const ['06:00 AM', '07:30 AM'],
      maxCapacityPerSlot: 12,
      bookedSlotsMap: {
        BookingSchedule.slotKey(now.add(const Duration(days: 2)), '06:00 AM'):
            11,
      },
      rating: 4.7,
      reviewsCount: 84,
    ),
    BookingSchedule(
      id: '00000000-0000-0000-0001-000000000004',
      destinationId: '00000000-0000-0000-0002-000000000001',
      destinationTitle: "Little Adam's Peak & Tea Factory Experience",
      location: 'Ella, Badulla District',
      guideName: 'Niroshan Perera',
      pricePerPerson: 3800.0,
      availableDates: [
        now.add(const Duration(days: 1)),
        now.add(const Duration(days: 3)),
        now.add(const Duration(days: 5)),
      ],
      availableTimeSlots: const ['08:30 AM', '11:00 AM', '03:00 PM'],
      maxCapacityPerSlot: 6,
      bookedSlotsMap: {},
      rating: 4.9,
      reviewsCount: 110,
    ),
    BookingSchedule(
      id: '00000000-0000-0000-0001-000000000005',
      destinationId: '00000000-0000-0000-0002-000000000002',
      destinationTitle: 'Pidurangala Rock Sunset & Village Cycle Tour',
      location: 'Sigiriya, Matale District',
      guideName: 'Sunil Wickramasinghe',
      pricePerPerson: 4200.0,
      availableDates: [
        now.add(const Duration(days: 2)),
        now.add(const Duration(days: 4)),
        now.add(const Duration(days: 7)),
      ],
      availableTimeSlots: const ['07:00 AM', '04:00 PM'],
      maxCapacityPerSlot: 8,
      bookedSlotsMap: {},
      rating: 4.6,
      reviewsCount: 52,
    ),
  ];
}

// ── Schedule Filter State ────────────────────────────────────────────────────

class ScheduleFilterState {
  final String searchQuery;
  final String? destination;
  final DateTime? filterDate;

  const ScheduleFilterState({
    this.searchQuery = '',
    this.destination,
    this.filterDate,
  });

  ScheduleFilterState copyWith({
    String? searchQuery,
    String? destination,
    DateTime? filterDate,
    bool clearDestination = false,
    bool clearFilterDate = false,
  }) {
    return ScheduleFilterState(
      searchQuery: searchQuery ?? this.searchQuery,
      destination: clearDestination ? null : (destination ?? this.destination),
      filterDate: clearFilterDate ? null : (filterDate ?? this.filterDate),
    );
  }
}

final scheduleFilterProvider = StateProvider<ScheduleFilterState>(
  (ref) => const ScheduleFilterState(),
);

// ── 1. scheduleListProvider → GET /api/booking-schedules ─────────────────────

class ScheduleListNotifier
    extends StateNotifier<AsyncValue<List<BookingSchedule>>> {
  ScheduleListNotifier() : super(const AsyncValue.loading()) {
    loadSchedules();
  }

  Future<void> loadSchedules({String? search, DateTime? date}) async {
    state = const AsyncValue.loading();
    try {
      final raw = await bookingApiService.getSchedules(
        search: search,
        date: date,
      );
      final schedules = raw.map(_scheduleFromJson).toList();
      state = AsyncValue.data(schedules);
    } on DioException catch (e) {
      debugPrint('[BookingAPI] Schedules fetch failed: $e — using seed data');
      state = AsyncValue.data(_seedSchedules());
    } catch (e, st) {
      state = AsyncValue.error(e, st);
    }
  }

  void updateSlotCapacity(
    String scheduleId,
    DateTime date,
    String slot,
    int guestsAdded,
  ) {
    state.whenData((schedules) {
      final updated = schedules.map((sch) {
        if (sch.id == scheduleId) {
          final key = BookingSchedule.slotKey(date, slot);
          final current = sch.bookedSlotsMap[key] ?? 0;
          final updatedMap = Map<String, int>.from(sch.bookedSlotsMap);
          updatedMap[key] = current + guestsAdded;
          return sch.copyWith(bookedSlotsMap: updatedMap);
        }
        return sch;
      }).toList();
      state = AsyncValue.data(updated);
    });
  }
}

final scheduleListProvider =
    StateNotifierProvider<
      ScheduleListNotifier,
      AsyncValue<List<BookingSchedule>>
    >((ref) {
      return ScheduleListNotifier();
    });

/// Filtered schedules derived from the raw list + current filter state.
final filteredSchedulesProvider = Provider<AsyncValue<List<BookingSchedule>>>((
  ref,
) {
  final schedulesAsync = ref.watch(scheduleListProvider);
  final filter = ref.watch(scheduleFilterProvider);

  return schedulesAsync.whenData((schedules) {
    return schedules.where((schedule) {
      final matchesQuery =
          filter.searchQuery.isEmpty ||
          schedule.destinationTitle.toLowerCase().contains(
            filter.searchQuery.toLowerCase(),
          ) ||
          schedule.location.toLowerCase().contains(
            filter.searchQuery.toLowerCase(),
          ) ||
          schedule.guideName.toLowerCase().contains(
            filter.searchQuery.toLowerCase(),
          );

      final matchesDest =
          filter.destination == null ||
          filter.destination!.isEmpty ||
          schedule.location.toLowerCase().contains(
            filter.destination!.toLowerCase(),
          );

      final matchesDate =
          filter.filterDate == null ||
          schedule.availableDates.any(
            (d) =>
                d.year == filter.filterDate!.year &&
                d.month == filter.filterDate!.month &&
                d.day == filter.filterDate!.day,
          );

      return matchesQuery && matchesDest && matchesDate;
    }).toList();
  });
});

// ── 3. travelerBookingsProvider → GET /api/bookings/traveler/{id} ─────────────

class TravelerBookingsNotifier
    extends StateNotifier<AsyncValue<List<Booking>>> {
  final Ref ref;
  TravelerBookingsNotifier(this.ref) : super(const AsyncValue.loading()) {
    _load();
  }

  Future<void> _load() async {
    state = const AsyncValue.loading();
    try {
      final travelerId = ref.read(currentTravelerProvider)['id']!;
      final raw = await bookingApiService.getTravelerBookings(travelerId);
      state = AsyncValue.data(raw.map(_bookingFromJson).toList());
    } on DioException catch (e) {
      debugPrint(
        '[BookingAPI] Traveler bookings fetch failed: $e — using seed',
      );
      _seedFallback();
    } catch (e, st) {
      state = AsyncValue.error(e, st);
    }
  }

  void _seedFallback() {
    final now = DateTime.now();
    state = AsyncValue.data([
      Booking(
        id: '00000000-0000-0000-0003-000000000001',
        scheduleId: '00000000-0000-0000-0001-000000000001',
        destinationTitle: 'Ella Rock & Nine Arch Bridge Trek',
        location: 'Ella, Badulla District',
        travelerId: '00000000-0000-0000-0000-000000000001',
        travelerName: 'Nithu Traveler',
        travelerEmail: 'traveler@travyle.com',
        date: now.add(const Duration(days: 3)),
        timeSlot: '09:00 AM',
        guests: 2,
        basePrice: 9000.0,
        serviceFee: 450.0,
        discountAmount: 0.0,
        totalAmount: 9450.0,
        status: BookingStatus.confirmed,
        paymentStatus: EscrowStatus.heldInEscrow,
        paymentMethod: BookingPaymentMethod.sampleCard,
        createdAt: now.subtract(const Duration(days: 1)),
        transactionRef: 'pi_test_stripe_10101',
        escrowReleaseDate: now.add(const Duration(days: 4)),
      ),
      Booking(
        id: '00000000-0000-0000-0003-000000000002',
        scheduleId: '00000000-0000-0000-0001-000000000002',
        destinationTitle: 'Sigiriya Ancient Rock Fortress Sunrise Tour',
        location: 'Sigiriya, Matale District',
        travelerId: '00000000-0000-0000-0000-000000000001',
        travelerName: 'Nithu Traveler',
        travelerEmail: 'traveler@travyle.com',
        date: now.subtract(const Duration(days: 5)),
        timeSlot: '05:30 AM',
        guests: 1,
        basePrice: 6000.0,
        serviceFee: 300.0,
        discountAmount: 500.0,
        totalAmount: 5800.0,
        status: BookingStatus.completed,
        paymentStatus: EscrowStatus.released,
        paymentMethod: BookingPaymentMethod.sampleCard,
        createdAt: now.subtract(const Duration(days: 10)),
        transactionRef: 'pi_test_stripe_10102',
        escrowReleaseDate: now.subtract(const Duration(days: 4)),
      ),
    ]);
  }

  void addBooking(Booking newBooking) {
    state.whenData((list) {
      state = AsyncValue.data([newBooking, ...list]);
    });
  }

  void updateBooking(Booking updated) {
    state.whenData((list) {
      state = AsyncValue.data(
        list.map((b) => b.id == updated.id ? updated : b).toList(),
      );
    });
  }

  void removeBooking(String bookingId) {
    state.whenData((list) {
      state = AsyncValue.data(list.where((b) => b.id != bookingId).toList());
    });
  }

  void cancelBooking(String bookingId) {
    state.whenData((list) {
      state = AsyncValue.data(
        list.map((b) {
          if (b.id == bookingId) {
            return b.copyWith(
              status: BookingStatus.cancelled,
              paymentStatus: b.paymentStatus == EscrowStatus.heldInEscrow
                  ? EscrowStatus.refunded
                  : b.paymentStatus,
            );
          }
          return b;
        }).toList(),
      );
    });
  }

  Future<void> refresh() => _load();
}

final travelerBookingsProvider =
    StateNotifierProvider<TravelerBookingsNotifier, AsyncValue<List<Booking>>>((
      ref,
    ) {
      return TravelerBookingsNotifier(ref);
    });

// ── 4. bookingDetailProvider → derived from travelerBookings ─────────────────

final bookingDetailProvider = Provider.family<Booking?, String>((
  ref,
  bookingId,
) {
  final bookings = ref.watch(travelerBookingsProvider).valueOrNull;
  if (bookings == null) return null;
  try {
    return bookings.firstWhere((b) => b.id == bookingId);
  } catch (_) {
    return null;
  }
});

// ── 2. createBookingProvider → POST /api/bookings ────────────────────────────

class CreateBookingState {
  final bool isLoading;
  final String? error;
  final Booking? createdBooking;

  const CreateBookingState({
    this.isLoading = false,
    this.error,
    this.createdBooking,
  });
}

class CreateBookingNotifier extends StateNotifier<CreateBookingState> {
  final Ref ref;
  CreateBookingNotifier(this.ref) : super(const CreateBookingState());

  Future<Booking?> createBooking({
    required BookingSchedule schedule,
    required DateTime date,
    required String timeSlot,
    required int guests,
    required double basePrice,
    required double serviceFee,
    required double discountAmount,
    required double totalAmount,
    String? notes,
    required BookingPaymentMethod paymentMethod,
    String? receiptReference,
    String? receiptImageData,
  }) async {
    state = const CreateBookingState(isLoading: true);

    // Client-side capacity guard (also enforced server-side)
    if (schedule.isSlotFull(date, timeSlot, requestedGuests: guests)) {
      final remaining = schedule.remainingCapacity(date, timeSlot);
      final errorMsg = remaining == 0
          ? 'Selected slot is completely full. Please choose another.'
          : 'Only $remaining spot(s) remaining, but $guests requested.';
      state = CreateBookingState(isLoading: false, error: errorMsg);
      return null;
    }

    final traveler = ref.read(currentTravelerProvider);

    try {
      final json = await bookingApiService.createBooking(
        scheduleId: schedule.id,
        travelerId: traveler['id']!,
        travelerName: traveler['name']!,
        travelerEmail: traveler['email']!,
        bookingDate: date,
        timeSlot: timeSlot,
        guests: guests,
        notes: notes,
        paymentMethod: paymentMethod.apiValue,
        receiptReference: receiptReference,
        receiptImageData: receiptImageData,
      );

      final booking = _bookingFromJson(json);

      // Update local slot map so UI reflects the new booking immediately
      ref
          .read(scheduleListProvider.notifier)
          .updateSlotCapacity(schedule.id, date, timeSlot, guests);

      ref.read(travelerBookingsProvider.notifier).addBooking(booking);
      state = CreateBookingState(isLoading: false, createdBooking: booking);
      return booking;
    } on DioException catch (e) {
      // Handle 422 Unprocessable (slot full according to server)
      String errorMsg = 'Booking failed. Please try again.';
      if (e.response?.statusCode == 422) {
        errorMsg = (e.response?.data as Map?)?['error'] as String? ?? errorMsg;
      }
      debugPrint('[BookingAPI] createBooking error: $e');

      // A server response means the request reached the API. Do not turn a
      // rejected booking, especially a full slot, into a local success.
      if (e.response != null) {
        state = CreateBookingState(isLoading: false, error: errorMsg);
        return null;
      }

      // Offline fallback: create booking locally so demo still works
      final bookingId =
          'BKG-${DateTime.now().millisecondsSinceEpoch.toString().substring(7)}';
      final booking = Booking(
        id: bookingId,
        scheduleId: schedule.id,
        destinationTitle: schedule.destinationTitle,
        location: schedule.location,
        travelerId: traveler['id']!,
        travelerName: traveler['name']!,
        travelerEmail: traveler['email']!,
        date: date,
        timeSlot: timeSlot,
        guests: guests,
        basePrice: basePrice,
        serviceFee: serviceFee,
        discountAmount: discountAmount,
        totalAmount: totalAmount,
        status: BookingStatus.pending,
        paymentStatus: EscrowStatus.pending,
        paymentMethod: paymentMethod,
        createdAt: DateTime.now(),
        notes: notes,
        receiptReference: receiptReference,
        receiptImageData: receiptImageData,
      );
      ref
          .read(scheduleListProvider.notifier)
          .updateSlotCapacity(schedule.id, date, timeSlot, guests);
      ref.read(travelerBookingsProvider.notifier).addBooking(booking);
      state = CreateBookingState(isLoading: false, createdBooking: booking);
      return booking;
    } catch (e) {
      state = CreateBookingState(isLoading: false, error: e.toString());
      return null;
    }
  }
}

final createBookingProvider =
    StateNotifierProvider<CreateBookingNotifier, CreateBookingState>((ref) {
      return CreateBookingNotifier(ref);
    });

// ── 5. updateBookingStatusProvider → PUT /api/bookings/{id}/status ───────────

class UpdateBookingStatusNotifier extends StateNotifier<AsyncValue<void>> {
  final Ref ref;
  UpdateBookingStatusNotifier(this.ref) : super(const AsyncValue.data(null));

  Future<bool> updateStatus(String bookingId, BookingStatus newStatus) async {
    state = const AsyncValue.loading();
    try {
      final json = await bookingApiService.updateBookingStatus(
        bookingId,
        newStatus.label,
      );
      if (json != null) {
        ref
            .read(travelerBookingsProvider.notifier)
            .updateBooking(_bookingFromJson(json));
      }
      state = const AsyncValue.data(null);
      return json != null;
    } on DioException catch (_) {
      // Offline fallback: update locally
      final existing = ref.read(bookingDetailProvider(bookingId));
      if (existing != null) {
        ref
            .read(travelerBookingsProvider.notifier)
            .updateBooking(existing.copyWith(status: newStatus));
      }
      state = const AsyncValue.data(null);
      return existing != null;
    } catch (e, st) {
      state = AsyncValue.error(e, st);
      return false;
    }
  }
}

final updateBookingStatusProvider =
    StateNotifierProvider<UpdateBookingStatusNotifier, AsyncValue<void>>((ref) {
      return UpdateBookingStatusNotifier(ref);
    });

// ── 6. cancelBookingProvider → DELETE /api/bookings/{id} ────────────────────

class CancelBookingNotifier extends StateNotifier<AsyncValue<void>> {
  final Ref ref;
  CancelBookingNotifier(this.ref) : super(const AsyncValue.data(null));

  Future<bool> cancel(String bookingId) async {
    state = const AsyncValue.loading();
    try {
      final ok = await bookingApiService.cancelBooking(bookingId);
      if (ok) {
        ref.read(travelerBookingsProvider.notifier).cancelBooking(bookingId);
      }
      state = const AsyncValue.data(null);
      return ok;
    } on DioException catch (_) {
      // Offline fallback
      ref.read(travelerBookingsProvider.notifier).cancelBooking(bookingId);
      state = const AsyncValue.data(null);
      return true;
    } catch (e, st) {
      state = AsyncValue.error(e, st);
      return false;
    }
  }
}

final cancelBookingProvider =
    StateNotifierProvider<CancelBookingNotifier, AsyncValue<void>>((ref) {
      return CancelBookingNotifier(ref);
    });

// ── 7. createDiscountRequestProvider → POST /api/discount-requests ───────────

class DiscountRequestState {
  final bool isSubmitting;
  final String? error;
  final DiscountRequest? submitted;

  const DiscountRequestState({
    this.isSubmitting = false,
    this.error,
    this.submitted,
  });
}

class DiscountRequestNotifier extends StateNotifier<DiscountRequestState> {
  final Ref ref;
  DiscountRequestNotifier(this.ref) : super(const DiscountRequestState());

  Future<DiscountRequest?> submitRequest({
    required String bookingId,
    required String scheduleId,
    required double originalPrice,
    required double requestedDiscountPercent,
    required String reason,
  }) async {
    state = const DiscountRequestState(isSubmitting: true);

    final traveler = ref.read(currentTravelerProvider);

    try {
      final json = await bookingApiService.createDiscountRequest(
        bookingId: bookingId,
        scheduleId: scheduleId,
        travelerId: traveler['id']!,
        originalPrice: originalPrice,
        requestedDiscountPercent: requestedDiscountPercent,
        reason: reason,
      );

      final discountReq = DiscountRequest(
        id: json['id'].toString(),
        bookingId: json['bookingId'].toString(),
        scheduleId: json['scheduleId'].toString(),
        travelerId: json['travelerId'].toString(),
        originalPrice: (json['originalPrice'] as num).toDouble(),
        requestedDiscountPercent: (json['requestedDiscountPercent'] as num)
            .toDouble(),
        reason: json['reason'] as String,
        status: _parseDiscountStatus(json['status'] as String),
        createdAt: DateTime.parse(json['createdAt'] as String),
      );

      state = DiscountRequestState(isSubmitting: false, submitted: discountReq);
      ref.read(discountRequestsProvider.notifier).refresh();
      return discountReq;
    } on DioException catch (e) {
      debugPrint('[BookingAPI] createDiscountRequest error: $e');
      // Offline fallback
      final discountReq = DiscountRequest(
        id: 'DISC-${DateTime.now().millisecondsSinceEpoch.toString().substring(8)}',
        bookingId: bookingId,
        scheduleId: scheduleId,
        travelerId: traveler['id']!,
        originalPrice: originalPrice,
        requestedDiscountPercent: requestedDiscountPercent,
        reason: reason,
        status: DiscountStatus.pending,
        createdAt: DateTime.now(),
      );
      state = DiscountRequestState(isSubmitting: false, submitted: discountReq);
      return discountReq;
    } catch (e) {
      state = DiscountRequestState(isSubmitting: false, error: e.toString());
      return null;
    }
  }
}

final createDiscountRequestProvider =
    StateNotifierProvider<DiscountRequestNotifier, DiscountRequestState>((ref) {
      return DiscountRequestNotifier(ref);
    });

class DiscountRequestsNotifier
    extends StateNotifier<AsyncValue<List<DiscountRequest>>> {
  final Ref ref;

  DiscountRequestsNotifier(this.ref) : super(const AsyncValue.loading()) {
    refresh();
  }

  Future<void> refresh() async {
    state = const AsyncValue.loading();
    final travelerId = ref.read(currentTravelerProvider)['id']!;
    try {
      final json = await bookingApiService.getTravelerDiscountRequests(
        travelerId,
      );
      state = AsyncValue.data(json.map(_discountRequestFromJson).toList());
    } on DioException catch (error, stackTrace) {
      state = AsyncValue.error(error, stackTrace);
    } catch (error, stackTrace) {
      state = AsyncValue.error(error, stackTrace);
    }
  }
}

final discountRequestsProvider =
    StateNotifierProvider<
      DiscountRequestsNotifier,
      AsyncValue<List<DiscountRequest>>
    >((ref) {
      return DiscountRequestsNotifier(ref);
    });

// ── 8. processEscrowPaymentProvider → POST /api/bookings/{id}/process-escrow-payment

class EscrowPaymentState {
  final bool isProcessing;
  final String? error;
  final PaymentEscrow? escrow;

  const EscrowPaymentState({
    this.isProcessing = false,
    this.error,
    this.escrow,
  });
}

class EscrowPaymentNotifier extends StateNotifier<EscrowPaymentState> {
  final Ref ref;
  EscrowPaymentNotifier(this.ref) : super(const EscrowPaymentState());

  Future<PaymentEscrow?> processEscrowPayment({
    required String bookingId,
    required double totalAmount,
    required String paymentMethodId,
  }) async {
    state = const EscrowPaymentState(isProcessing: true);

    try {
      final json = await bookingApiService.processEscrowPayment(
        bookingId: bookingId,
        totalAmount: totalAmount,
        paymentMethodId: paymentMethodId,
      );

      final escrow = PaymentEscrow(
        id: json['id'].toString(),
        bookingId: json['bookingId'].toString(),
        amount: (json['amount'] as num).toDouble(),
        status: _parseEscrowStatus(json['status'] as String),
        transactionRef: json['transactionRef'] as String,
        createdAt: DateTime.parse(json['createdAt'] as String),
        escrowReleaseDate: DateTime.parse(json['escrowReleaseDate'] as String),
      );

      // Mark booking as confirmed + held in escrow locally
      final existing = ref.read(bookingDetailProvider(bookingId));
      if (existing != null) {
        ref
            .read(travelerBookingsProvider.notifier)
            .updateBooking(
              existing.copyWith(
                status: BookingStatus.confirmed,
                paymentStatus: EscrowStatus.heldInEscrow,
                transactionRef: escrow.transactionRef,
                escrowReleaseDate: escrow.escrowReleaseDate,
              ),
            );
      }

      state = EscrowPaymentState(isProcessing: false, escrow: escrow);
      return escrow;
    } on DioException catch (e) {
      debugPrint('[BookingAPI] processEscrowPayment error: $e');
      // Offline fallback
      final escrow = PaymentEscrow(
        id: 'ESC-${DateTime.now().millisecondsSinceEpoch.toString().substring(7)}',
        bookingId: bookingId,
        amount: totalAmount,
        status: EscrowStatus.heldInEscrow,
        transactionRef: 'txn_offline_${paymentMethodId.substring(0, 8)}',
        createdAt: DateTime.now(),
        escrowReleaseDate: DateTime.now().add(const Duration(days: 3)),
      );
      final existing = ref.read(bookingDetailProvider(bookingId));
      if (existing != null) {
        ref
            .read(travelerBookingsProvider.notifier)
            .updateBooking(
              existing.copyWith(
                status: BookingStatus.confirmed,
                paymentStatus: EscrowStatus.heldInEscrow,
                transactionRef: escrow.transactionRef,
                escrowReleaseDate: escrow.escrowReleaseDate,
              ),
            );
      }
      state = EscrowPaymentState(isProcessing: false, escrow: escrow);
      return escrow;
    } catch (e) {
      state = EscrowPaymentState(isProcessing: false, error: e.toString());
      return null;
    }
  }
}

final processEscrowPaymentProvider =
    StateNotifierProvider<EscrowPaymentNotifier, EscrowPaymentState>((ref) {
      return EscrowPaymentNotifier(ref);
    });
