import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:firebase_core/firebase_core.dart';
import 'package:mobile/screens/guide_dashboard_screen.dart';

void main() {
  setUpAll(() async {
    TestWidgetsFlutterBinding.ensureInitialized();

    await Firebase.initializeApp();
  });

  testWidgets('GuideDashboardScreen shows loading indicator initially',
      (WidgetTester tester) async {
    await tester.pumpWidget(
      const MaterialApp(
        home: GuideDashboardScreen(),
      ),
    );

    expect(find.byType(CircularProgressIndicator), findsOneWidget);
  });
}