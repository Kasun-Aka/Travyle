class AuditLogModel {
  final String id;
  final String? supportTicketId;
  final String action;
  final String actorRole;
  final String? actorId;
  final String details;
  final String? metadataJson;
  final DateTime timestamp;

  AuditLogModel({
    required this.id,
    this.supportTicketId,
    required this.action,
    required this.actorRole,
    this.actorId,
    required this.details,
    this.metadataJson,
    required this.timestamp,
  });

  factory AuditLogModel.fromJson(Map<String, dynamic> json) {
    return AuditLogModel(
      id: json['id'] ?? '',
      supportTicketId: json['supportTicketId'],
      action: json['action'] ?? '',
      actorRole: json['actorRole'] ?? '',
      actorId: json['actorId'],
      details: json['details'] ?? '',
      metadataJson: json['metadataJson'],
      timestamp: DateTime.tryParse(json['timestamp'] ?? '') ?? DateTime.now(),
    );
  }
}

class SupportTicketModel {
  final String id;
  final String userId;
  final String? userName;
  final String? userEmail;
  final String? bookingId;
  final String? tourId;
  final String title;
  final String description;
  final String category;
  final String priority;
  final String status;
  final String? attachmentUrl;
  final double sentimentScore;
  final String severityTier;
  final String? aiReasoning;
  final String? resolutionSummary;
  final DateTime createdAt;
  final DateTime updatedAt;
  final List<AuditLogModel> auditLogs;

  SupportTicketModel({
    required this.id,
    required this.userId,
    this.userName,
    this.userEmail,
    this.bookingId,
    this.tourId,
    required this.title,
    required this.description,
    required this.category,
    required this.priority,
    required this.status,
    this.attachmentUrl,
    required this.sentimentScore,
    required this.severityTier,
    this.aiReasoning,
    this.resolutionSummary,
    required this.createdAt,
    required this.updatedAt,
    this.auditLogs = const [],
  });

  factory SupportTicketModel.fromJson(Map<String, dynamic> json) {
    return SupportTicketModel(
      id: json['id'] ?? '',
      userId: json['userId'] ?? '',
      userName: json['userName'],
      userEmail: json['userEmail'],
      bookingId: json['bookingId'],
      tourId: json['tourId'],
      title: json['title'] ?? '',
      description: json['description'] ?? '',
      category: json['category'] ?? 'General',
      priority: json['priority'] ?? 'Medium',
      status: json['status'] ?? 'Pending_AI_Triage',
      attachmentUrl: json['attachmentUrl'],
      sentimentScore: (json['sentimentScore'] as num?)?.toDouble() ?? 0.0,
      severityTier: json['severityTier'] ?? 'Tier_1_Low',
      aiReasoning: json['aiReasoning'],
      resolutionSummary: json['resolutionSummary'],
      createdAt: DateTime.tryParse(json['createdAt'] ?? '') ?? DateTime.now(),
      updatedAt: DateTime.tryParse(json['updatedAt'] ?? '') ?? DateTime.now(),
      auditLogs: (json['auditLogs'] as List<dynamic>?)
              ?.map((e) => AuditLogModel.fromJson(e as Map<String, dynamic>))
              .toList() ??
          [],
    );
  }
}
