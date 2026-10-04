import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mobile/features/bookings/models/booking.dart';
import 'package:mobile/features/bookings/providers/booking_providers.dart';
import 'package:mobile/features/bookings/screens/booking_checkout_screen.dart';
import 'package:mobile/features/bookings/screens/booking_history_screen.dart';
import 'package:mobile/features/bookings/screens/booking_status_screen.dart';
import 'package:mobile/features/bookings/screens/schedule_browse_screen.dart';
import 'package:mobile/features/bookings/screens/slot_selection_screen.dart';
import 'package:mobile/features/bookings/widgets/booking_status_tracker.dart';
import 'package:mobile/features/bookings/widgets/empty_state.dart';
import 'package:mobile/features/bookings/widgets/schedule_card.dart';

BookingSchedule _narrowSchedule() => BookingSchedule(
  id: 'narrow-schedule',
  destinationId: 'colombo-zoo',
  destinationTitle: 'Colombo Zoo',
  location: 'Colombo',
  guideName: 'Kasun Bandara',
  pricePerPerson: 2000,
  availableDates: [DateTime(2026, 9, 30)],
  availableTimeSlots: const ['12:00AM'],
  maxCapacityPerSlot: 8,
  bookedSlotsMap: const {},
  rating: 4.9,
  reviewsCount: 142,
);

Booking _narrowBooking() => Booking(
  id: 'long-booking-reference-123456789',
  scheduleId: 'narrow-schedule',
  destinationTitle: 'Colombo Zoo',
  location: 'Colombo',
  travelerId: 'traveler-id',
  travelerName: 'Nithu Traveler',
  travelerEmail: 'nithu.traveler@example.com',
  date: DateTime(2026, 9, 30),
  timeSlot: '12:00AM',
  guests: 3,
  basePrice: 6000,
  serviceFee: 300,
  discountAmount: 0,
  totalAmount: 6300,
  status: BookingStatus.confirmed,
  paymentStatus: EscrowStatus.heldInEscrow,
  paymentMethod: BookingPaymentMethod.sampleCard,
  createdAt: DateTime(2026, 9, 29),
  transactionRef: 'txn_sandbox_pm_very_long_reference_123456789',
);

void main() {
  Widget createTestWidget(Widget child, {List<Override> overrides = const []}) {
    return ProviderScope(
      overrides: overrides,
      child: MaterialApp(home: child),
    );
  }

  group('BookingStatusTracker Widget Tests', () {
    testWidgets('renders Pending step as current when status is pending', (
      tester,
    ) async {
      await tester.pumpWidget(
        createTestWidget(
          const Scaffold(
            body: BookingStatusTracker(status: BookingStatus.pending),
          ),
        ),
      );

      expect(
        find.text('Pending'),
        findsNWidgets(2),
      ); // in header chip and stepper step
      expect(find.text('Booking requested'), findsOneWidget);
    });

    testWidgets('renders Confirmed step with Escrow secured text', (
      tester,
    ) async {
      await tester.pumpWidget(
        createTestWidget(
          const Scaffold(
            body: BookingStatusTracker(
              status: BookingStatus.confirmed,
              escrowStatus: EscrowStatus.heldInEscrow,
            ),
          ),
        ),
      );

      expect(find.text('Confirmed'), findsNWidgets(2));
      expect(find.text('Escrow secured'), findsOneWidget);
    });

    testWidgets('renders special banner when booking is cancelled', (
      tester,
    ) async {
      await tester.pumpWidget(
        createTestWidget(
          const Scaffold(
            body: BookingStatusTracker(status: BookingStatus.cancelled),
          ),
        ),
      );

      expect(find.text('Booking Cancelled'), findsOneWidget);
      expect(find.byIcon(Icons.cancel_rounded), findsOneWidget);
    });
  });

  group('ScheduleBrowseScreen Widget Tests', () {
    testWidgets('renders schedule browse screen with list of tours', (
      tester,
    ) async {
      await tester.pumpWidget(createTestWidget(const ScheduleBrowseScreen()));
      await tester.pumpAndSettle();

      expect(find.text('Explore Schedules'), findsOneWidget);
      expect(find.byType(TextField), findsOneWidget);
      expect(find.text('Ella Rock & Nine Arch Bridge Trek'), findsOneWidget);
    });

    testWidgets('fits a narrow phone viewport without layout overflow', (
      tester,
    ) async {
      tester.view.physicalSize = const Size(320, 800);
      tester.view.devicePixelRatio = 1.0;
      addTearDown(tester.view.resetPhysicalSize);
      addTearDown(tester.view.resetDevicePixelRatio);

      await tester.pumpWidget(createTestWidget(const ScheduleBrowseScreen()));
      await tester.pumpAndSettle();

      expect(find.text('Explore Schedules'), findsOneWidget);
      expect(tester.takeException(), isNull);
    });

    testWidgets('reflows schedule card at narrow phone width', (tester) async {
      tester.view.physicalSize = const Size(320, 800);
      tester.view.devicePixelRatio = 1.0;
      addTearDown(tester.view.resetPhysicalSize);
      addTearDown(tester.view.resetDevicePixelRatio);

      final schedule = BookingSchedule(
        id: 'narrow-test',
        destinationId: 'ella',
        destinationTitle: 'Ella Rock & Nine Arch Bridge Trek',
        location: 'Ella, Badulla District',
        guideName: 'Kasun Bandara',
        pricePerPerson: 8500,
        availableDates: [DateTime(2026, 9, 30)],
        availableTimeSlots: const ['06:30 AM', '09:00 AM'],
        maxCapacityPerSlot: 8,
        bookedSlotsMap: const {},
        rating: 4.9,
        reviewsCount: 142,
      );

      await tester.pumpWidget(
        createTestWidget(
          Scaffold(
            body: Padding(
              padding: const EdgeInsets.symmetric(horizontal: 12),
              child: ScheduleCard(schedule: schedule, onSelect: () {}),
            ),
          ),
        ),
      );
      await tester.pumpAndSettle();

      expect(find.text('Select Slot'), findsOneWidget);
      expect(tester.takeException(), isNull);
    });

    testWidgets('renders empty state when search matches nothing', (
      tester,
    ) async {
      await tester.pumpWidget(createTestWidget(const ScheduleBrowseScreen()));
      await tester.pumpAndSettle();

      // Enter search term with no match
      await tester.enterText(find.byType(TextField), 'NonExistentTourXYZ');
      await tester.pumpAndSettle();

      expect(find.byType(EmptyState), findsOneWidget);
      expect(find.text('No schedules found'), findsOneWidget);
    });
  });

  group('Narrow Booking Screen Tests', () {
    void setNarrowViewport(WidgetTester tester) {
      tester.view.physicalSize = const Size(321, 738);
      tester.view.devicePixelRatio = 1.0;
      addTearDown(tester.view.resetPhysicalSize);
      addTearDown(tester.view.resetDevicePixelRatio);
    }

    testWidgets('checkout fits a narrow phone viewport', (tester) async {
      setNarrowViewport(tester);
      await tester.pumpWidget(
        createTestWidget(
          BookingCheckoutScreen(
            schedule: _narrowSchedule(),
            selectedDate: DateTime(2026, 9, 30),
            selectedSlot: '12:00AM',
            guests: 3,
          ),
          overrides: [
            currentTravelerProvider.overrideWith(
              (ref) async => {
                'id': 'traveler-id',
                'name': 'Nithu Traveler',
                'email': 'nithu.traveler@example.com',
              },
            ),
          ],
        ),
      );
      await tester.pumpAndSettle();

      expect(find.text('Checkout & Escrow'), findsOneWidget);
      expect(find.textContaining('Pay LKR'), findsOneWidget);
      expect(tester.takeException(), isNull);
    });

    testWidgets('booking status fits a narrow phone viewport', (tester) async {
      setNarrowViewport(tester);
      final booking = _narrowBooking();
      await tester.pumpWidget(
        createTestWidget(
          BookingStatusScreen(bookingId: booking.id),
          overrides: [
            bookingDetailProvider(booking.id).overrideWith((ref) => booking),
          ],
        ),
      );
      await tester.pumpAndSettle();

      expect(find.text('Tour Details'), findsOneWidget);
      expect(tester.takeException(), isNull);
    });

    testWidgets('saved bookings fits a narrow phone viewport', (tester) async {
      setNarrowViewport(tester);
      await tester.pumpWidget(createTestWidget(const BookingHistoryScreen()));
      await tester.pumpAndSettle();

      expect(find.text('All'), findsOneWidget);
      expect(find.text('Completed'), findsOneWidget);
      expect(tester.takeException(), isNull);
    });
  });

  group('SlotSelectionScreen Widget Tests', () {
    testWidgets('disables fully booked slot and prevents selecting it', (
      tester,
    ) async {
      tester.view.physicalSize = const Size(800, 1400);
      tester.view.devicePixelRatio = 1.0;
      addTearDown(tester.view.resetPhysicalSize);
      addTearDown(tester.view.resetDevicePixelRatio);

      final testDate = DateTime(2026, 8, 1);
      final testSchedule = BookingSchedule(
        id: 'sch-test',
        destinationId: 'dest-mirissa',
        destinationTitle: 'Whale Watching Tour',
        location: 'Mirissa',
        guideName: 'Chaminda',
        pricePerPerson: 7000.0,
        availableDates: [testDate],
        availableTimeSlots: const ['06:00 AM', '09:00 AM'],
        maxCapacityPerSlot: 5,
        bookedSlotsMap: {
          BookingSchedule.slotKey(testDate, '06:00 AM'):
              5, // 0 remaining -> FULL
          BookingSchedule.slotKey(testDate, '09:00 AM'): 2, // 3 remaining
        },
        rating: 4.8,
        reviewsCount: 22,
      );

      await tester.pumpWidget(
        createTestWidget(SlotSelectionScreen(schedule: testSchedule)),
      );
      await tester.pumpAndSettle();

      // Check that 'FULL' badge is displayed for 06:00 AM
      expect(find.text('FULL'), findsOneWidget);
      expect(find.text('3 spots left'), findsOneWidget);

      // Tap on the full slot (06:00 AM)
      await tester.tap(find.text('06:00 AM'));
      await tester.pumpAndSettle();

      // Because 06:00 AM was full, the active selected slot should remain 09:00 AM
      expect(find.text('09:00 AM'), findsOneWidget);
    });
  });
}
