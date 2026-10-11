import 'package:flutter/material.dart';
import 'pdf_viewer_screen.dart';
import 'package:flutter_map/flutter_map.dart';
import 'package:latlong2/latlong.dart';
import 'package:dio/dio.dart';
import 'package:firebase_auth/firebase_auth.dart';

import '../theme/app_theme.dart';
import '../widgets/gradient_button.dart';
import '../services/api_config.dart';
import '../features/destinations/models/destination.dart';
import '../features/bookings/screens/schedule_browse_screen.dart';

class DestinationDetailScreen extends StatelessWidget {
  final Destination destination;

  const DestinationDetailScreen({super.key, required this.destination});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: Stack(
        children: [
          // Background Hero Image
          Positioned(
            top: 0,
            left: 0,
            right: 0,
            height: MediaQuery.of(context).size.height * 0.45,
            child: Image.network(
              destination.imageUrl,
              fit: BoxFit.cover,
              errorBuilder: (_, __, ___) => const ColoredBox(
                color: AppTheme.borderLight,
                child: Icon(Icons.landscape, size: 72),
              ),
            ),
          ),

          // Custom Back Button & Bookmark
          Positioned(
            top: MediaQuery.of(context).padding.top + 16,
            left: 20,
            right: 20,
            child: Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                _buildCircleIconButton(
                  icon: Icons.arrow_back,
                  onPressed: () => Navigator.pop(context),
                ),
                _buildCircleIconButton(
                  icon: Icons.bookmark_border,
                  onPressed: () {},
                ),
              ],
            ),
          ),

          // Scrollable Content
          DraggableScrollableSheet(
            initialChildSize: 0.65,
            minChildSize: 0.65,
            maxChildSize: 0.9,
            builder: (context, scrollController) {
              return Stack(
                clipBehavior: Clip.none,
                children: [
                  Positioned.fill(
                    top: 48,
                    child: Container(
                      decoration: const BoxDecoration(
                        color: AppTheme.backgroundLight,
                        borderRadius: BorderRadius.only(
                          topLeft: Radius.circular(32),
                          topRight: Radius.circular(32),
                        ),
                      ),
                    ),
                  ),
                  Positioned.fill(
                    child: SingleChildScrollView(
                      controller: scrollController,
                      padding: const EdgeInsets.symmetric(horizontal: 24.0),
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          // Floating Title Card
                          Container(
                            margin: const EdgeInsets.only(bottom: 24),
                            padding: const EdgeInsets.all(20),
                            decoration: BoxDecoration(
                              color: Colors.white,
                              borderRadius: BorderRadius.circular(20),
                              boxShadow: [
                                BoxShadow(
                                  color: Colors.black.withOpacity(0.08),
                                  blurRadius: 20,
                                  offset: const Offset(0, 10),
                                ),
                              ],
                            ),
                            child: Row(
                              mainAxisAlignment: MainAxisAlignment.spaceBetween,
                              children: [
                                Expanded(
                                  child: Column(
                                    crossAxisAlignment:
                                        CrossAxisAlignment.start,
                                    children: [
                                      Text(
                                        destination.name,
                                        style: const TextStyle(
                                          fontSize: 22,
                                          fontWeight: FontWeight.bold,
                                          color: AppTheme.primaryDark,
                                        ),
                                      ),
                                      const SizedBox(height: 4),
                                      Text(
                                        destination.region,
                                        style: TextStyle(
                                          color: AppTheme.textGrey
                                              .withOpacity(0.8),
                                          fontSize: 13,
                                        ),
                                      ),
                                    ],
                                  ),
                                ),
                                const SizedBox(width: 16),
                                Column(
                                  crossAxisAlignment: CrossAxisAlignment.end,
                                  children: [
                                    Row(
                                      children: [
                                        const Icon(
                                          Icons.star,
                                          color: Colors.amber,
                                          size: 18,
                                        ),
                                        const SizedBox(width: 4),
                                        Text(
                                          destination.averageRating
                                              .toStringAsFixed(1),
                                          style: const TextStyle(
                                            fontWeight: FontWeight.bold,
                                            fontSize: 16,
                                            color: AppTheme.textDark,
                                          ),
                                        ),
                                      ],
                                    ),
                                    const SizedBox(height: 4),
                                    Text(
                                      'Reviews',
                                      style: TextStyle(
                                        color: AppTheme.textGrey
                                            .withOpacity(0.8),
                                        fontSize: 12,
                                      ),
                                    ),
                                  ],
                                ),
                              ],
                            ),
                          ),

                          // Main Content
                          Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              const Text(
                                'OVERVIEW',
                                style: TextStyle(
                                  fontWeight: FontWeight.bold,
                                  fontSize: 13,
                                  letterSpacing: 1.2,
                                  color: AppTheme.primaryDark,
                                ),
                              ),
                              const SizedBox(height: 12),
                              Text(
                                destination.description.isNotEmpty
                                    ? destination.description
                                    : 'No description available.',
                                style: const TextStyle(
                                  fontSize: 15,
                                  color: AppTheme.textGrey,
                                  height: 1.5,
                                ),
                              ),
                              const SizedBox(height: 24),

                              const Text(
                                'HIGHLIGHTS',
                                style: TextStyle(
                                  fontWeight: FontWeight.bold,
                                  fontSize: 13,
                                  letterSpacing: 1.2,
                                  color: AppTheme.textGrey,
                                ),
                              ),
                              const SizedBox(height: 12),
                              Wrap(
                                spacing: 8,
                                runSpacing: 8,
                                children: destination.tags
                                    .map((tag) =>
                                        _buildHighlightChip(tag, true))
                                    .toList(),
                              ),
                              const SizedBox(height: 32),

                              // AI Planner Card
                              Container(
                                padding: const EdgeInsets.all(20),
                                decoration: BoxDecoration(
                                  color: Colors.white,
                                  borderRadius: BorderRadius.circular(20),
                                  border: Border.all(
                                    color: AppTheme.borderLight,
                                  ),
                                ),
                                child: Column(
                                  crossAxisAlignment: CrossAxisAlignment.start,
                                  children: [
                                    const Row(
                                      children: [
                                        Icon(
                                          Icons.auto_awesome,
                                          color: AppTheme.primaryDark,
                                        ),
                                        SizedBox(width: 8),
                                        Text(
                                          'Wanderlust AI Planner',
                                          style: TextStyle(
                                            fontWeight: FontWeight.bold,
                                            fontSize: 16,
                                            color: AppTheme.primaryDark,
                                          ),
                                        ),
                                      ],
                                    ),
                                    const SizedBox(height: 12),
                                    const Text(
                                      'Receive a meticulously-tailored, multi-day smart itinerary optimized for your style.',
                                      style: TextStyle(
                                        color: AppTheme.textGrey,
                                        fontSize: 14,
                                      ),
                                    ),
                                    const SizedBox(height: 20),
                                    GradientButton(
                                      text: 'GENERATE AI ITINERARY',
                                      onPressed: () =>
                                          _generateAiItinerary(context),
                                    ),
                                  ],
                                ),
                              ),
                              const SizedBox(height: 32),

                              const Text(
                                'MAP AREA',
                                style: TextStyle(
                                  fontWeight: FontWeight.bold,
                                  fontSize: 13,
                                  letterSpacing: 1.2,
                                  color: AppTheme.primaryDark,
                                ),
                              ),
                              const SizedBox(height: 12),
                              ClipRRect(
                                borderRadius: BorderRadius.circular(16),
                                child: SizedBox(
                                  height: 200,
                                  width: double.infinity,
                                  child: FlutterMap(
                                    options: MapOptions(
                                      initialCenter: LatLng(
                                        destination.latitude != 0
                                            ? destination.latitude
                                            : 7.8731,
                                        destination.longitude != 0
                                            ? destination.longitude
                                            : 80.7718,
                                      ),
                                      initialZoom: 12,
                                      interactionOptions:
                                          const InteractionOptions(
                                        flags: InteractiveFlag.all &
                                            ~InteractiveFlag.rotate,
                                      ),
                                    ),
                                    children: [
                                      TileLayer(
                                        urlTemplate:
                                            'https://tile.openstreetmap.org/{z}/{x}/{y}.png',
                                        userAgentPackageName:
                                            'com.travyle.app',
                                      ),
                                      if (destination.latitude != 0 &&
                                          destination.longitude != 0)
                                        MarkerLayer(
                                          markers: [
                                            Marker(
                                              point: LatLng(
                                                destination.latitude,
                                                destination.longitude,
                                              ),
                                              width: 40,
                                              height: 40,
                                              child: const Icon(
                                                Icons.location_on,
                                                color: AppTheme.primaryDark,
                                                size: 40,
                                              ),
                                            ),
                                          ],
                                        ),
                                    ],
                                  ),
                                ),
                              ),
                              const SizedBox(height: 32),
                            ],
                          ),
                        ],
                      ),
                    ),
                  ),
                ],
              );
            },
          ),
        ],
      ),
      bottomNavigationBar: Container(
        padding: const EdgeInsets.symmetric(horizontal: 24.0, vertical: 20.0),
        decoration: BoxDecoration(
          color: Colors.white,
          border: Border(
            top: BorderSide(color: AppTheme.borderLight.withOpacity(0.5)),
          ),
        ),
        child: Row(
          mainAxisAlignment: MainAxisAlignment.spaceBetween,
          children: [
            Column(
              mainAxisSize: MainAxisSize.min,
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const Text(
                  'GUIDED TOUR',
                  style: TextStyle(
                    color: AppTheme.textGrey,
                    fontSize: 10,
                    letterSpacing: 1,
                  ),
                ),
                Row(
                  crossAxisAlignment: CrossAxisAlignment.end,
                  children: [
                    const Text(
                      '\$2,850',
                      style: TextStyle(
                        fontSize: 24,
                        fontWeight: FontWeight.bold,
                        color: AppTheme.primaryDark,
                      ),
                    ),
                    const SizedBox(width: 4),
                    Text(
                      '/ pax',
                      style: TextStyle(
                        fontSize: 14,
                        color: AppTheme.textGrey.withOpacity(0.8),
                        height: 1.8,
                      ),
                    ),
                  ],
                ),
              ],
            ),
            ElevatedButton(
              onPressed: () {
                Navigator.of(context).push(
                  MaterialPageRoute(
                    builder: (_) => const ScheduleBrowseScreen(),
                  ),
                );
              },
              style: ElevatedButton.styleFrom(
                backgroundColor: AppTheme.primaryDark,
                padding: const EdgeInsets.symmetric(
                  horizontal: 32,
                  vertical: 16,
                ),
                shape: RoundedRectangleBorder(
                  borderRadius: BorderRadius.circular(30),
                ),
              ),
              child: const Text(
                'BOOK TOUR',
                style: TextStyle(
                  fontWeight: FontWeight.bold,
                  color: Colors.white,
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildCircleIconButton({
    required IconData icon,
    required VoidCallback onPressed,
  }) {
    return Container(
      decoration: const BoxDecoration(
        color: Colors.white,
        shape: BoxShape.circle,
      ),
      child: IconButton(
        icon: Icon(icon, color: AppTheme.primaryDark),
        onPressed: onPressed,
      ),
    );
  }

  Widget _buildHighlightChip(String label, bool isSelected) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
      decoration: BoxDecoration(
        color: isSelected ? AppTheme.primaryDark : Colors.transparent,
        border: Border.all(
          color: isSelected ? AppTheme.primaryDark : AppTheme.borderLight,
        ),
        borderRadius: BorderRadius.circular(20),
      ),
      child: Text(
        label,
        style: TextStyle(
          color: isSelected ? Colors.white : AppTheme.primaryDark,
          fontWeight: FontWeight.w600,
          fontSize: 13,
        ),
      ),
    );
  }

  Future<void> _generateAiItinerary(BuildContext context) async {
    final user = FirebaseAuth.instance.currentUser;
    if (user == null || user.email == null) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
            content: Text('Please log in to generate itineraries.')),
      );
      return;
    }

    showDialog(
      context: context,
      barrierDismissible: false,
      builder: (context) => const Center(
        child: CircularProgressIndicator(color: AppTheme.primaryDark),
      ),
    );

    try {
      final dio = Dio(
        BaseOptions(
          connectTimeout: const Duration(seconds: 45),
          receiveTimeout: const Duration(seconds: 60),
        ),
      );

      final response = await dio.post(
        '${ApiConfig.apiBaseUrl}/agent/itinerary',
        data: {
          'email': user.email,
          'destinationId': destination.id,
        },
      );

      final itineraryData = response.data;
      String? savedPlanId;

      try {
        final saveResponse = await dio.post(
          '${ApiConfig.apiBaseUrl}/travel-plans',
          data: {
            'email': user.email,
            'title': 'Trip to ${destination.name}',
            'destinationId': destination.id,
            'durationDays': 3,
            'estimatedBudget': 500.0,
            'aiItineraryData': itineraryData
          },
        );
        savedPlanId = saveResponse.data['id'];
      } catch (e) {
        print('Failed to save travel plan: $e');
      }

      if (context.mounted) {
        Navigator.pop(context); // Close loading dialog
        _showItineraryBottomSheet(context, itineraryData, savedPlanId);
      }
    } catch (e) {
      if (context.mounted) {
        Navigator.pop(context); // Close loading dialog
        String message = 'Failed to generate AI itinerary.';
        if (e is DioException) {
          if (e.response?.statusCode == 500) {
            message = 'Backend AI service error (500). Please check backend deployment & Gemini configuration.';
          } else if (e.type == DioExceptionType.connectionTimeout || e.type == DioExceptionType.receiveTimeout) {
            message = 'AI itinerary generation timed out. Please try again.';
          } else if (e.response?.data != null && e.response!.data.toString().trim().isNotEmpty) {
            message = e.response!.data.toString();
          } else {
            message = 'Network error: ${e.message ?? 'connection failed'}.';
          }
        }
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text(message)),
        );
      }
    }
  }

  void _showItineraryBottomSheet(BuildContext context, dynamic data, String? planId) {
    List<dynamic> days = [];
    if (data is List) {
      days = data;
    } else if (data is Map && data.containsKey('days')) {
      days = data['days'];
    } else {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Invalid AI format received')),
      );
      return;
    }

    showModalBottomSheet(
      context: context,
      isScrollControlled: true,
      backgroundColor: Colors.transparent,
      builder: (context) => Container(
        height: MediaQuery.of(context).size.height * 0.8,
        decoration: const BoxDecoration(
          color: Colors.white,
          borderRadius: BorderRadius.vertical(top: Radius.circular(24)),
        ),
        padding: const EdgeInsets.all(24),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                const Icon(Icons.auto_awesome, color: AppTheme.primaryDark),
                const SizedBox(width: 8),
                Expanded(
                  child: Text(
                    'Your Smart Itinerary - ${destination.name}',
                    style: const TextStyle(
                      fontSize: 20,
                      fontWeight: FontWeight.bold,
                      color: AppTheme.primaryDark,
                    ),
                  ),
                ),
            if (planId != null)
                    IconButton(
                      icon: const Icon(Icons.picture_as_pdf, color: AppTheme.primaryDark),
                      tooltip: 'Download PDF',
                      onPressed: () async {
                        try {
                          final dio = Dio();
                          final pdfResponse = await dio.get(
                            '${ApiConfig.apiBaseUrl}/travel-plans/$planId/pdf',
                            options: Options(responseType: ResponseType.bytes),
                          );
                          if (context.mounted) {
                            Navigator.push(
                              context,
                              MaterialPageRoute(
                                builder: (context) => PdfViewerScreen(pdfBytes: pdfResponse.data),
                              ),
                            );
                          }
                        } catch (e) {
                          ScaffoldMessenger.of(context).showSnackBar(
                            SnackBar(content: Text('Failed to download PDF: $e')),
                          );
                        }
                      },
                    ),
                      IconButton(
                  icon: const Icon(Icons.close),
                  onPressed: () => Navigator.pop(context),
                ),
              ],
            ),
            const SizedBox(height: 16),
            Expanded(
              child: ListView.builder(
                itemCount: days.length,
                itemBuilder: (context, index) {
                  final dayData = days[index];
                  return Container(
                    margin: const EdgeInsets.only(bottom: 16),
                    padding: const EdgeInsets.all(16),
                    decoration: BoxDecoration(
                      color: AppTheme.backgroundLight,
                      borderRadius: BorderRadius.circular(16),
                      border: Border.all(color: AppTheme.borderLight),
                    ),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          'Day ${dayData['day'] ?? (index + 1)}: ${dayData['title'] ?? ''}',
                          style: const TextStyle(
                            fontWeight: FontWeight.bold,
                            fontSize: 16,
                            color: AppTheme.primaryDark,
                          ),
                        ),
                        const SizedBox(height: 8),
                        Text(
                          dayData['description'] ?? '',
                          style: const TextStyle(
                            color: AppTheme.textGrey,
                            height: 1.5,
                          ),
                        ),
                      ],
                    ),
                  );
                },
              ),
            ),
          ],
        ),
      ),
    );
  }
}