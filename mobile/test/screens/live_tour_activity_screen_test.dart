import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mobile/screens/live_tour_activity_screen.dart';

void main() {
  testWidgets('LiveTourActivityScreen renders loading state initially', (WidgetTester tester) async {
    await tester.pumpWidget(
      const MaterialApp(
        home: LiveTourActivityScreen(isGuide: true),
      ),
    );

    // Should display the loading indicator text initially since _isLoadingData is true
    expect(find.text('Loading...'), findsOneWidget);
  });
}
