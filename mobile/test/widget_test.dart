import 'package:flutter_test/flutter_test.dart';
import 'package:mobile/models/support_ticket.dart';
import 'package:mobile/models/voucher.dart';
import 'package:mobile/models/customer_review.dart';

void main() {
  group('Component 4 Mobile Data Models Unit Tests (Support & Vouchers)', () {
    test('SupportTicketModel fromJson and properties parsing', () {
      final json = {
        'id': 'st-101',
        'userId': 'usr-1',
        'userName': 'John Doe',
        'title': 'Dispute Tour Delay',
        'description': 'Tour bus arrived 2 hours late',
        'category': 'Dispute',
        'priority': 'High',
        'status': 'Open',
        'sentimentScore': -0.8,
        'severityTier': 'Tier_3_High',
        'createdAt': '2026-10-08T10:00:00Z',
        'updatedAt': '2026-10-08T10:00:00Z',
      };

      final ticket = SupportTicketModel.fromJson(json);

      expect(ticket.id, 'st-101');
      expect(ticket.title, 'Dispute Tour Delay');
      expect(ticket.status, 'Open');
      expect(ticket.priority, 'High');
      expect(ticket.severityTier, 'Tier_3_High');
    });

    test('VoucherModel fromJson and active validity check', () {
      final json = {
        'id': 'vch-888',
        'code': 'TRAV-GW-50',
        'userId': 'usr-1',
        'amount': 50.0,
        'reason': 'Tour disruption goodwill',
        'status': 'Active',
        'expiresAt': '2027-01-01T00:00:00Z',
        'createdAt': '2026-10-08T10:00:00Z',
      };

      final voucher = VoucherModel.fromJson(json);

      expect(voucher.id, 'vch-888');
      expect(voucher.code, 'TRAV-GW-50');
      expect(voucher.amount, 50.0);
      expect(voucher.status, 'Active');
    });

    test('CustomerReviewModel rating validation', () {
      final json = {
        'id': 'rev-55',
        'tourId': 'dest-1',
        'userId': 'usr-2',
        'userName': 'Sarah',
        'rating': 5,
        'comment': 'Awesome tour experience!',
        'isVerified': true,
        'createdAt': '2026-10-08T12:00:00Z',
      };

      final review = CustomerReviewModel.fromJson(json);

      expect(review.id, 'rev-55');
      expect(review.rating, 5);
      expect(review.comment, contains('Awesome'));
      expect(review.isVerified, isTrue);
    });

    test('AuditLogModel fromJson parsing', () {
      final json = {
        'id': 'aud-001',
        'supportTicketId': 'st-101',
        'action': 'VoucherApproved',
        'actorRole': 'Admin',
        'actorId': 'admin-01',
        'details': 'Issued \$50 goodwill voucher',
        'timestamp': '2026-10-08T14:00:00Z',
      };

      final audit = AuditLogModel.fromJson(json);

      expect(audit.id, 'aud-001');
      expect(audit.action, 'VoucherApproved');
      expect(audit.actorRole, 'Admin');
    });
  });
}