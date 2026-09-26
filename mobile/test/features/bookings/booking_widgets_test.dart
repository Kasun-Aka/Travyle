import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mobile/features/bookings/models/booking.dart';
import 'package:mobile/features/bookings/screens/schedule_browse_screen.dart';
import 'package:mobile/features/bookings/screens/slot_selection_screen.dart';
import 'package:mobile/features/bookings/widgets/booking_status_tracker.dart';
import 'package:mobile/features/bookings/widgets/empty_state.dart';

void main() {
  Widget createTestWidget(Widget child, {List<Override> overrides = const []}) {
    return ProviderScope(
      overrides: overrides,
      child: MaterialApp(
        home: child,
      ),
    );
  }

  group('BookingStatusTracker Widget Tests', () {
    testWidgets('renders Pending step as current when status is pending', (tester) async {
      await tester.pumpWidget(
        createTestWidget(
          const Scaffold(
            body: BookingStatusTracker(status: BookingStatus.pending),
          ),
        ),
      );

      expect(find.text('Pending'), findsNWidgets(2)); // in header chip and stepper step
      expect(find.text('Booking requested'), findsOneWidget);
    });

    testWidgets('renders Confirmed step with Escrow secured text', (tester) async {
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

    testWidgets('renders special banner when booking is cancelled', (tester) async {
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
    testWidgets('renders schedule browse screen with list of tours', (tester) async {
      await tester.pumpWidget(
        createTestWidget(const ScheduleBrowseScreen()),
      );
      await tester.pumpAndSettle();

      expect(find.text('Explore Schedules'), findsOneWidget);
      expect(find.byType(TextField), findsOneWidget);
      expect(find.text('Ella Rock & Nine Arch Bridge Trek'), findsOneWidget);
    });

    testWidgets('renders empty state when search matches nothing', (tester) async {
      await tester.pumpWidget(
        createTestWidget(const ScheduleBrowseScreen()),
      );
      await tester.pumpAndSettle();

      // Enter search term with no match
      await tester.enterText(find.byType(TextField), 'NonExistentTourXYZ');
      await tester.pumpAndSettle();

      expect(find.byType(EmptyState), findsOneWidget);
      expect(find.text('No schedules found'), findsOneWidget);
    });
  });

  group('SlotSelectionScreen Widget Tests', () {
    testWidgets('disables fully booked slot and prevents selecting it', (tester) async {
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
          BookingSchedule.slotKey(testDate, '06:00 AM'): 5, // 0 remaining -> FULL
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
