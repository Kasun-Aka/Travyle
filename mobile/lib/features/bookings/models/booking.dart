// Plain Dart domain models for Student 2: Traveler Bookings & Financial Escrow.
// NOTE: BookingSchedule.rating and reviewsCount are purely static numeric fields
// for display only. Full reviews/ratings lifecycle belongs to Student 4.

enum BookingStatus {
  pending,
  confirmed,
  completed,
  cancelled;

  String get label {
    switch (this) {
      case BookingStatus.pending:
        return 'Pending';
      case BookingStatus.confirmed:
        return 'Confirmed';
      case BookingStatus.completed:
        return 'Completed';
      case BookingStatus.cancelled:
        return 'Cancelled';
    }
  }
}

enum EscrowStatus {
  pending,
  heldInEscrow,
  released,
  refunded;

  String get label {
    switch (this) {
      case EscrowStatus.pending:
        return 'Payment Pending';
      case EscrowStatus.heldInEscrow:
        return 'Held in Escrow';
      case EscrowStatus.released:
        return 'Released to Guide';
      case EscrowStatus.refunded:
        return 'Refunded to Traveler';
    }
  }
}

enum BookingPaymentMethod {
  sampleCard,
  bankTransferReceipt,
  atmCashReceipt;

  String get apiValue {
    switch (this) {
      case BookingPaymentMethod.sampleCard:
        return 'SampleCard';
      case BookingPaymentMethod.bankTransferReceipt:
        return 'BankTransferReceipt';
      case BookingPaymentMethod.atmCashReceipt:
        return 'AtmCashReceipt';
    }
  }
}

enum DiscountStatus {
  pending,
  approved,
  rejected;

  String get label {
    switch (this) {
      case DiscountStatus.pending:
        return 'Under Review';
      case DiscountStatus.approved:
        return 'Approved';
      case DiscountStatus.rejected:
        return 'Declined';
    }
  }

  String get userLabel {
    switch (this) {
      case DiscountStatus.pending:
        return 'Processing';
      case DiscountStatus.approved:
        return 'Refunded';
      case DiscountStatus.rejected:
        return 'Cancelled';
    }
  }
}

class Booking {
  final String id;
  final String scheduleId;
  final String destinationTitle;
  final String location;
  final String travelerId;
  final String travelerName;
  final String travelerEmail;
  final DateTime date;
  final String timeSlot;
  final int guests;
  final double basePrice;
  final double serviceFee;
  final double discountAmount;
  final double totalAmount;
  final BookingStatus status;
  final EscrowStatus paymentStatus;
  final BookingPaymentMethod paymentMethod;
  final DateTime createdAt;
  final String? notes;
  final String? transactionRef;
  final String? receiptReference;
  final String? receiptImageData;
  final DateTime? escrowReleaseDate;

  const Booking({
    required this.id,
    required this.scheduleId,
    required this.destinationTitle,
    required this.location,
    required this.travelerId,
    required this.travelerName,
    required this.travelerEmail,
    required this.date,
    required this.timeSlot,
    required this.guests,
    required this.basePrice,
    required this.serviceFee,
    required this.discountAmount,
    required this.totalAmount,
    required this.status,
    required this.paymentStatus,
    required this.paymentMethod,
    required this.createdAt,
    this.notes,
    this.transactionRef,
    this.receiptReference,
    this.receiptImageData,
    this.escrowReleaseDate,
  });

  Booking copyWith({
    String? id,
    String? scheduleId,
    String? destinationTitle,
    String? location,
    String? travelerId,
    String? travelerName,
    String? travelerEmail,
    DateTime? date,
    String? timeSlot,
    int? guests,
    double? basePrice,
    double? serviceFee,
    double? discountAmount,
    double? totalAmount,
    BookingStatus? status,
    EscrowStatus? paymentStatus,
    BookingPaymentMethod? paymentMethod,
    DateTime? createdAt,
    String? notes,
    String? transactionRef,
    String? receiptReference,
    String? receiptImageData,
    DateTime? escrowReleaseDate,
  }) {
    return Booking(
      id: id ?? this.id,
      scheduleId: scheduleId ?? this.scheduleId,
      destinationTitle: destinationTitle ?? this.destinationTitle,
      location: location ?? this.location,
      travelerId: travelerId ?? this.travelerId,
      travelerName: travelerName ?? this.travelerName,
      travelerEmail: travelerEmail ?? this.travelerEmail,
      date: date ?? this.date,
      timeSlot: timeSlot ?? this.timeSlot,
      guests: guests ?? this.guests,
      basePrice: basePrice ?? this.basePrice,
      serviceFee: serviceFee ?? this.serviceFee,
      discountAmount: discountAmount ?? this.discountAmount,
      totalAmount: totalAmount ?? this.totalAmount,
      status: status ?? this.status,
      paymentStatus: paymentStatus ?? this.paymentStatus,
      paymentMethod: paymentMethod ?? this.paymentMethod,
      createdAt: createdAt ?? this.createdAt,
      notes: notes ?? this.notes,
      transactionRef: transactionRef ?? this.transactionRef,
      receiptReference: receiptReference ?? this.receiptReference,
      receiptImageData: receiptImageData ?? this.receiptImageData,
      escrowReleaseDate: escrowReleaseDate ?? this.escrowReleaseDate,
    );
  }
}

class BookingSchedule {
  final String id;
  final String destinationId;
  final String destinationTitle;
  final String location;
  final String guideName;
  final double pricePerPerson;
  final List<DateTime> availableDates;
  final List<String> availableTimeSlots;
  // Exact slots returned for each date, including admin slot overrides.
  final Map<String, List<String>> slotsByDate;
  final int maxCapacityPerSlot;
  // Map of date-slot key ("YYYY-MM-DD_HH:MM") to currently booked count
  final Map<String, int> bookedSlotsMap;
  // Static numeric display fields only — Student 4 owns the full reviews system
  final double rating;
  final int reviewsCount;

  const BookingSchedule({
    required this.id,
    required this.destinationId,
    required this.destinationTitle,
    required this.location,
    required this.guideName,
    required this.pricePerPerson,
    required this.availableDates,
    required this.availableTimeSlots,
    this.slotsByDate = const {},
    required this.maxCapacityPerSlot,
    required this.bookedSlotsMap,
    required this.rating,
    required this.reviewsCount,
  });

  static String slotKey(DateTime date, String slot) {
    final y = date.year.toString().padLeft(4, '0');
    final m = date.month.toString().padLeft(2, '0');
    final d = date.day.toString().padLeft(2, '0');
    return '$y-$m-${d}_$slot';
  }

  int bookedCount(DateTime date, String slot) {
    final key = slotKey(date, slot);
    return bookedSlotsMap[key] ?? 0;
  }

  List<String> slotsForDate(DateTime date) {
    final dateKey =
        '${date.year.toString().padLeft(4, '0')}-'
        '${date.month.toString().padLeft(2, '0')}-'
        '${date.day.toString().padLeft(2, '0')}';
    return slotsByDate[dateKey] ?? availableTimeSlots;
  }

  int remainingCapacity(DateTime date, String slot) {
    final booked = bookedCount(date, slot);
    final remaining = maxCapacityPerSlot - booked;
    return remaining < 0 ? 0 : remaining;
  }

  bool isSlotFull(DateTime date, String slot, {int requestedGuests = 1}) {
    return remainingCapacity(date, slot) < requestedGuests;
  }

  BookingSchedule copyWith({
    String? id,
    String? destinationId,
    String? destinationTitle,
    String? location,
    String? guideName,
    double? pricePerPerson,
    List<DateTime>? availableDates,
    List<String>? availableTimeSlots,
    Map<String, List<String>>? slotsByDate,
    int? maxCapacityPerSlot,
    Map<String, int>? bookedSlotsMap,
    double? rating,
    int? reviewsCount,
  }) {
    return BookingSchedule(
      id: id ?? this.id,
      destinationId: destinationId ?? this.destinationId,
      destinationTitle: destinationTitle ?? this.destinationTitle,
      location: location ?? this.location,
      guideName: guideName ?? this.guideName,
      pricePerPerson: pricePerPerson ?? this.pricePerPerson,
      availableDates: availableDates ?? this.availableDates,
      availableTimeSlots: availableTimeSlots ?? this.availableTimeSlots,
      slotsByDate: slotsByDate ?? this.slotsByDate,
      maxCapacityPerSlot: maxCapacityPerSlot ?? this.maxCapacityPerSlot,
      bookedSlotsMap: bookedSlotsMap ?? this.bookedSlotsMap,
      rating: rating ?? this.rating,
      reviewsCount: reviewsCount ?? this.reviewsCount,
    );
  }
}

class DiscountRequest {
  final String id;
  final String bookingId;
  final String scheduleId;
  final String travelerId;
  final double originalPrice;
  final double requestedDiscountPercent;
  final String reason;
  final DiscountStatus status;
  final DateTime createdAt;

  const DiscountRequest({
    required this.id,
    required this.bookingId,
    required this.scheduleId,
    required this.travelerId,
    required this.originalPrice,
    required this.requestedDiscountPercent,
    required this.reason,
    required this.status,
    required this.createdAt,
  });

  double get calculatedDiscountAmount =>
      (originalPrice * requestedDiscountPercent) / 100.0;

  DiscountRequest copyWith({
    String? id,
    String? bookingId,
    String? scheduleId,
    String? travelerId,
    double? originalPrice,
    double? requestedDiscountPercent,
    String? reason,
    DiscountStatus? status,
    DateTime? createdAt,
  }) {
    return DiscountRequest(
      id: id ?? this.id,
      bookingId: bookingId ?? this.bookingId,
      scheduleId: scheduleId ?? this.scheduleId,
      travelerId: travelerId ?? this.travelerId,
      originalPrice: originalPrice ?? this.originalPrice,
      requestedDiscountPercent:
          requestedDiscountPercent ?? this.requestedDiscountPercent,
      reason: reason ?? this.reason,
      status: status ?? this.status,
      createdAt: createdAt ?? this.createdAt,
    );
  }
}

class PaymentEscrow {
  final String id;
  final String bookingId;
  final double amount;
  final EscrowStatus status;
  final String transactionRef;
  final DateTime createdAt;
  final DateTime escrowReleaseDate;

  const PaymentEscrow({
    required this.id,
    required this.bookingId,
    required this.amount,
    required this.status,
    required this.transactionRef,
    required this.createdAt,
    required this.escrowReleaseDate,
  });
}
