import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mobile/features/bookings/models/booking.dart';
import 'package:mobile/features/bookings/providers/booking_providers.dart';

void main() {
  group('Booking Models & Capacity Logic Tests', () {
    late BookingSchedule schedule;
    final testDate = DateTime(2026, 7, 29);
    const testSlot = '08:00 AM';

    setUp(() {
      schedule = BookingSchedule(
        id: 'test-sch-1',
        destinationId: 'dest-ella',
        destinationTitle: 'Ella Rock Trek',
        location: 'Ella',
        guideName: 'Kasun',
        pricePerPerson: 4000.0,
        availableDates: [testDate],
        availableTimeSlots: const [testSlot, '11:00 AM'],
        maxCapacityPerSlot: 10,
        bookedSlotsMap: {
          BookingSchedule.slotKey(testDate, testSlot): 8, // 2 left
          BookingSchedule.slotKey(testDate, '11:00 AM'): 10, // Full
        },
        rating: 4.8,
        reviewsCount: 30,
      );
    });

    test('remainingCapacity returns correct available count', () {
      expect(schedule.remainingCapacity(testDate, testSlot), equals(2));
      expect(schedule.remainingCapacity(testDate, '11:00 AM'), equals(0));
      // Unbooked slot on new date defaults to max capacity
      expect(schedule.remainingCapacity(DateTime(2026, 7, 30), testSlot), equals(10));
    });

    test('isSlotFull correctly flags full and insufficient capacity slots', () {
      // 11:00 AM is completely full
      expect(schedule.isSlotFull(testDate, '11:00 AM'), isTrue);

      // 08:00 AM has 2 remaining: not full for 1 or 2 guests
      expect(schedule.isSlotFull(testDate, testSlot, requestedGuests: 1), isFalse);
      expect(schedule.isSlotFull(testDate, testSlot, requestedGuests: 2), isFalse);

      // But full for 3 requested guests
      expect(schedule.isSlotFull(testDate, testSlot, requestedGuests: 3), isTrue);
    });

    test('DiscountRequest calculatedDiscountAmount calculates percentage correctly', () {
      final discountReq = DiscountRequest(
        id: 'disc-1',
        bookingId: 'bkg-1',
        scheduleId: 'sch-1',
        travelerId: 'traveler-1',
        originalPrice: 10000.0,
        requestedDiscountPercent: 15.0,
        reason: 'Student discount',
        status: DiscountStatus.pending,
        createdAt: DateTime(2026, 7, 29),
      );

      expect(discountReq.calculatedDiscountAmount, equals(1500.0));
    });
  });

  group('Riverpod Booking Providers Tests', () {
    test('createBookingProvider fails if slot capacity is exceeded', () async {
      final container = ProviderContainer();
      addTearDown(container.dispose);

      final testDate = DateTime(2026, 7, 29);
      final fullSchedule = BookingSchedule(
        id: 'sch-full',
        destinationId: 'dest-1',
        destinationTitle: 'Full Tour',
        location: 'Kandy',
        guideName: 'Guide',
        pricePerPerson: 5000.0,
        availableDates: [testDate],
        availableTimeSlots: const ['09:00 AM'],
        maxCapacityPerSlot: 5,
        bookedSlotsMap: {
          BookingSchedule.slotKey(testDate, '09:00 AM'): 5, // 0 remaining
        },
        rating: 4.5,
        reviewsCount: 10,
      );

      final result = await container.read(createBookingProvider.notifier).createBooking(
            schedule: fullSchedule,
            date: testDate,
            timeSlot: '09:00 AM',
            guests: 1,
            basePrice: 5000.0,
            serviceFee: 250.0,
            discountAmount: 0,
            totalAmount: 5250.0,
          );

      expect(result, isNull);
      final state = container.read(createBookingProvider);
      expect(state.error, contains('full'));
    });

    test('createBookingProvider successfully books and deducts capacity', () async {
      final container = ProviderContainer();
      addTearDown(container.dispose);

      final schedules = container.read(scheduleListProvider).value!;
      final schedule = schedules.first;
      final date = schedule.availableDates.first;
      const slot = '04:30 PM'; // unbooked slot in seed

      final initialRemaining = schedule.remainingCapacity(date, slot);

      final booking = await container.read(createBookingProvider.notifier).createBooking(
            schedule: schedule,
            date: date,
            timeSlot: slot,
            guests: 2,
            basePrice: schedule.pricePerPerson * 2,
            serviceFee: 400.0,
            discountAmount: 0,
            totalAmount: schedule.pricePerPerson * 2 + 400.0,
          );

      expect(booking, isNotNull);
      expect(booking!.status, equals(BookingStatus.pending));

      // Capacity should now be deducted in scheduleListProvider
      final updatedSchedules = container.read(scheduleListProvider).value!;
      final updatedSchedule = updatedSchedules.firstWhere((s) => s.id == schedule.id);
      expect(updatedSchedule.remainingCapacity(date, slot), equals(initialRemaining - 2));
    });

    test('cancelBookingProvider marks booking cancelled and refunds escrow', () async {
      final container = ProviderContainer();
      addTearDown(container.dispose);

      // Seed has BKG-10101 in confirmed & heldInEscrow
      final success = await container
          .read(cancelBookingProvider.notifier)
          .cancel('BKG-10101');

      expect(success, isTrue);

      final booking = container.read(bookingDetailProvider('BKG-10101'));
      expect(booking, isNotNull);
      expect(booking!.status, equals(BookingStatus.cancelled));
      expect(booking.paymentStatus, equals(EscrowStatus.refunded));
    });

    test('createDiscountRequestProvider submits request and updates booking balance', () async {
      final container = ProviderContainer();
      addTearDown(container.dispose);

      final initialBooking = container.read(bookingDetailProvider('BKG-10101'))!;
      final originalTotal = initialBooking.totalAmount;

      final req = await container
          .read(createDiscountRequestProvider.notifier)
          .submitRequest(
            bookingId: 'BKG-10101',
            scheduleId: initialBooking.scheduleId,
            originalPrice: originalTotal,
            requestedDiscountPercent: 10.0,
            reason: 'University Student ID 24104376',
          );

      expect(req, isNotNull);
      expect(req!.status, equals(DiscountStatus.pending));
      expect(req.calculatedDiscountAmount, equals(originalTotal * 0.10));

      final updatedBooking = container.read(bookingDetailProvider('BKG-10101'))!;
      expect(updatedBooking.discountAmount, equals(req.calculatedDiscountAmount));
      expect(updatedBooking.totalAmount, equals(originalTotal - req.calculatedDiscountAmount));
    });
  });
}
