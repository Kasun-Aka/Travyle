import 'package:flutter/material.dart';
import '../theme/app_theme.dart';
import '../widgets/gradient_button.dart';

class DestinationDetailScreen extends StatelessWidget {
  final Map<String, dynamic> destination;

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
              destination['imageUrl'] ??
                  'https://images.unsplash.com/photo-1537996194471-e657df975ab4?w=800&h=800&fit=crop',
              fit: BoxFit.cover,
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
                                        destination['name'] ??
                                            'Unknown Destination',
                                        style: const TextStyle(
                                          fontSize: 22,
                                          fontWeight: FontWeight.bold,
                                          color: AppTheme.primaryDark,
                                        ),
                                      ),
                                      const SizedBox(height: 4),
                                      Text(
                                        destination['region'] ??
                                            'Unknown Region',
                                        style: TextStyle(
                                          color: AppTheme.textGrey.withOpacity(
                                            0.8,
                                          ),
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
                                          (destination['averageRating'] ?? 0.0)
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
                                        color: AppTheme.textGrey.withOpacity(
                                          0.8,
                                        ),
                                        fontSize: 12,
                                      ),
                                    ),
                                  ],
                                ),
                              ],
                            ),
                          ),

                          // Content starts here
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
                                destination['description'] ??
                                    'No description available.',
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
                                children: [
                                  _buildHighlightChip('Luxury Spas', true),
                                  _buildHighlightChip('Private Beaches', true),
                                  _buildHighlightChip(
                                    'Cultural Heritage',
                                    false,
                                  ),
                                ],
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
                                    Row(
                                      children: [
                                        const Icon(
                                          Icons.auto_awesome,
                                          color: AppTheme.primaryDark,
                                        ),
                                        const SizedBox(width: 8),
                                        const Text(
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
                                      onPressed: () {},
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
                                child: Image.network(
                                  'https://images.unsplash.com/photo-1524661135-423995f22d0b?w=800&h=400&fit=crop',
                                  height: 160,
                                  width: double.infinity,
                                  fit: BoxFit.cover,
                                ),
                              ),
                            ],
                          ), // closes inner column
                        ],
                      ), // closes outer column
                    ), // closes SingleChildScrollView
                  ), // closes Positioned.fill
                ],
              ); // closes Stack
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
              onPressed: () {},
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
}
