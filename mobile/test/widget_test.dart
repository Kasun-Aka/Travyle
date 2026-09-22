import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mobile/features/bookings/screens/schedule_browse_screen.dart';
import 'package:mobile/features/bookings/theme/booking_theme.dart';

void main() {
  testWidgets('Schedule browse screen launches smoke test', (WidgetTester tester) async {
    await tester.pumpWidget(
      MaterialApp(
        theme: BookingTheme.themeData,
        home: const ProviderScope(child: ScheduleBrowseScreen()),
      ),
    );
    await tester.pumpAndSettle();

    expect(find.text('Explore Schedules'), findsOneWidget);
  });
}
