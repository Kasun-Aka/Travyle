class Destination {
  final String id;
  final String name;
  final String region;
  final String description;
  final List<String> tags;
  final String imageUrl;
  final double averageRating;

  const Destination({
    required this.id,
    required this.name,
    required this.region,
    required this.description,
    required this.tags,
    required this.imageUrl,
    required this.averageRating,
  });

  factory Destination.fromJson(Map<String, dynamic> json) {
    return Destination(
      id: json['id'].toString(),
      name: json['name'] as String? ?? '',
      region: json['region'] as String? ?? '',
      description: json['description'] as String? ?? '',
      tags: List<String>.from(json['tags'] as List? ?? const []),
      imageUrl: json['imageUrl'] as String? ?? '',
      averageRating: (json['averageRating'] as num?)?.toDouble() ?? 0,
    );
  }
}
