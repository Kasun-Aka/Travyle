class CustomerReviewModel {
  final String id;
  final String userId;
  final String? userName;
  final String tourId;
  final int rating;
  final String comment;
  final bool isVerified;
  final DateTime createdAt;

  CustomerReviewModel({
    required this.id,
    required this.userId,
    this.userName,
    required this.tourId,
    required this.rating,
    required this.comment,
    required this.isVerified,
    required this.createdAt,
  });

  factory CustomerReviewModel.fromJson(Map<String, dynamic> json) {
    return CustomerReviewModel(
      id: json['id'] ?? '',
      userId: json['userId'] ?? '',
      userName: json['userName'],
      tourId: json['tourId'] ?? '',
      rating: json['rating'] ?? 5,
      comment: json['comment'] ?? '',
      isVerified: json['isVerified'] ?? true,
      createdAt: DateTime.tryParse(json['createdAt'] ?? '') ?? DateTime.now(),
    );
  }
}
