import 'package:flutter_test/flutter_test.dart';
import 'package:mobile/models/support_ticket.dart';
import 'package:mobile/models/voucher.dart';
import 'package:mobile/models/customer_review.dart';

void main() {
  group('Component 4 - Support & Quality Models Test', () {
    test('SupportTicketModel.fromJson deserializes correctly with AI triage data', () {
      final json = {
        'id': '44444444-4444-4444-4444-444444444444',
        'userId': '11111111-1111-1111-1111-111111111111',
        'userName': 'Sarah Jenkins',
        'title': '3-Hour Tour Delay in Alpine Excursion',
        'description': 'Our tour bus was delayed by over 3 hours.',
        'category': 'TourDelay',
        'priority': 'High',
        'status': 'Pending_Admin_Voucher_Approval',
        'sentimentScore': -0.78,
        'severityTier': 'Tier_2_High',
        'aiReasoning': 'Sentiment: -0.78 | Tier: Tier_2_High',
        'createdAt': '2026-09-21T10:00:00Z',
        'updatedAt': '2026-09-21T10:05:00Z',
        'auditLogs': [
          {
            'id': 'aaaa-bbbb',
            'action': 'AI_SENTIMENT_ANALYZED',
            'actorRole': 'Support_AI_Agent',
            'details': 'Sentiment score evaluated.',
            'timestamp': '2026-09-21T10:01:00Z'
          }
        ]
      };

      final ticket = SupportTicketModel.fromJson(json);

      expect(ticket.id, '44444444-4444-4444-4444-444444444444');
      expect(ticket.title, '3-Hour Tour Delay in Alpine Excursion');
      expect(ticket.sentimentScore, -0.78);
      expect(ticket.severityTier, 'Tier_2_High');
      expect(ticket.status, 'Pending_Admin_Voucher_Approval');
      expect(ticket.auditLogs.length, 1);
      expect(ticket.auditLogs[0].action, 'AI_SENTIMENT_ANALYZED');
    });

    test('VoucherModel.fromJson deserializes correctly', () {
      final json = {
        'id': '55555555-5555-5555-5555-555555555555',
        'code': 'TRAV-GW-7842-0921',
        'userId': '11111111-1111-1111-1111-111111111111',
        'amount': 50.00,
        'reason': 'Goodwill compensation for Alpine tour delay',
        'status': 'Active',
        'expiresAt': '2026-12-31T23:59:59Z',
        'createdAt': '2026-09-21T10:00:00Z',
        'updatedAt': '2026-09-21T10:00:00Z'
      };

      final voucher = VoucherModel.fromJson(json);

      expect(voucher.code, 'TRAV-GW-7842-0921');
      expect(voucher.amount, 50.00);
      expect(voucher.status, 'Active');
    });

    test('CustomerReviewModel.fromJson deserializes correctly', () {
      final json = {
        'id': '66666666-6666-6666-6666-666666666666',
        'userId': '11111111-1111-1111-1111-111111111111',
        'tourId': '33333333-3333-3333-3333-333333333333',
        'rating': 5,
        'comment': 'Outstanding guide and mountain scenery!',
        'isVerified': true,
        'createdAt': '2026-09-21T12:00:00Z'
      };

      final review = CustomerReviewModel.fromJson(json);

      expect(review.rating, 5);
      expect(review.isVerified, isTrue);
      expect(review.comment, 'Outstanding guide and mountain scenery!');
    });
  });
}
