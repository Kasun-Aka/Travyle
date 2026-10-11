import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mobile/screens/qr_checkin_screen.dart';

void main() {
  testWidgets('QRCheckinScreen renders title and instructions', (WidgetTester tester) async {
    // Build our app and trigger a frame.
    await tester.pumpWidget(
      const MaterialApp(
        home: QRCheckinScreen(),
      ),
    );

    // Verify that the title is present
    expect(find.text('QR Tour Check-In'), findsOneWidget);
    expect(find.text('Verify traveler tour pass'), findsOneWidget);

    // Verify instructions are present
    expect(find.text('HOW IT WORKS'), findsOneWidget);
    expect(find.textContaining('POINT YOUR CAMERA AT THE TRAVELER\'S QR PASS'), findsOneWidget);

    // Verify the back button icon is present (Icons.arrow_back_ios_new)
    expect(find.byIcon(Icons.arrow_back_ios_new), findsOneWidget);
  });
}
