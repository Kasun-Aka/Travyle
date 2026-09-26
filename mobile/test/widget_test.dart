import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mobile/models/support_ticket.dart';
import 'package:mobile/models/voucher.dart';
import 'package:mobile/models/customer_review.dart';

void main() {
  testWidgets('app loads landing screen', (WidgetTester tester) async {
    await tester.pumpWidget(const ProviderScope(child: TravyleApp()));
    await tester.pumpAndSettle();

    expect(find.text('TRAVYLE'), findsOneWidget);
    expect(find.text('SIGN IN'), findsOneWidget);
  });
}