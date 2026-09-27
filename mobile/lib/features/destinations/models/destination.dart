class Destination {
  final String id;
  final String name;
  final String region;
  final String description;
  final String imageUrl;
  final double averageRating;
  final List<String> tags;
  final double latitude;
  final double longitude;

  Destination({
    required this.id,
    required this.name,
    required this.region,
    required this.description,
    required this.imageUrl,
    required this.averageRating,
    required this.tags,
    this.latitude = 0.0,
    this.longitude = 0.0,
  });

  factory Destination.fromJson(Map<String, dynamic> json) {
    return Destination(
      id: json['id'] ?? '',
      name: json['name'] ?? '',
      region: json['region'] ?? '',
      description: json['description'] ?? '',
      imageUrl: json['imageUrl'] ?? '',
      averageRating: (json['averageRating'] as num?)?.toDouble() ?? 0.0,
      tags: List<String>.from(json['tags'] ?? []),
      latitude: (json['latitude'] as num?)?.toDouble() ?? 0.0,
      longitude: (json['longitude'] as num?)?.toDouble() ?? 0.0,
    );
  }
}