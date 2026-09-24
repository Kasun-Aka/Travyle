class VoucherModel {
  final String id;
  final String code;
  final String userId;
  final String? userName;
  final String? supportTicketId;
  final double amount;
  final String reason;
  final String status;
  final String? approvedByAdminId;
  final DateTime? issuedAt;
  final DateTime? expiresAt;
  final DateTime? redeemedAt;
  final DateTime createdAt;

  VoucherModel({
    required this.id,
    required this.code,
    required this.userId,
    this.userName,
    this.supportTicketId,
    required this.amount,
    required this.reason,
    required this.status,
    this.approvedByAdminId,
    this.issuedAt,
    this.expiresAt,
    this.redeemedAt,
    required this.createdAt,
  });

  factory VoucherModel.fromJson(Map<String, dynamic> json) {
    return VoucherModel(
      id: json['id'] ?? '',
      code: json['code'] ?? '',
      userId: json['userId'] ?? '',
      userName: json['userName'],
      supportTicketId: json['supportTicketId'],
      amount: (json['amount'] as num?)?.toDouble() ?? 50.0,
      reason: json['reason'] ?? 'Goodwill compensation',
      status: json['status'] ?? 'Draft',
      approvedByAdminId: json['approvedByAdminId'],
      issuedAt: json['issuedAt'] != null ? DateTime.tryParse(json['issuedAt']) : null,
      expiresAt: json['expiresAt'] != null ? DateTime.tryParse(json['expiresAt']) : null,
      redeemedAt: json['redeemedAt'] != null ? DateTime.tryParse(json['redeemedAt']) : null,
      createdAt: DateTime.tryParse(json['createdAt'] ?? '') ?? DateTime.now(),
    );
  }
}
