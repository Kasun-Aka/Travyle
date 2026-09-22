import 'package:flutter/material.dart';
import '../theme/app_theme.dart';
import '../widgets/gradient_button.dart';

class PreferenceScreen extends StatefulWidget {
  const PreferenceScreen({super.key});

  @override
  State<PreferenceScreen> createState() => _PreferenceScreenState();
}

class _PreferenceScreenState extends State<PreferenceScreen> {
  final List<Map<String, dynamic>> _preferences = [
    {'title': 'Adventure', 'icon': Icons.explore_outlined, 'selected': true, 'image': 'https://images.unsplash.com/photo-1534447677768-be436bb09401?w=400&h=300&fit=crop'},
    {'title': 'Beach & Islands', 'icon': Icons.wb_sunny_outlined, 'selected': true, 'image': 'https://images.unsplash.com/photo-1507525428034-b723cf961d3e?w=400&h=300&fit=crop'},
    {'title': 'Cultural Heritage', 'icon': Icons.account_balance_outlined, 'selected': false, 'image': 'https://images.unsplash.com/photo-1548013146-72479768bada?w=400&h=300&fit=crop'},
    {'title': 'Food & Wine', 'icon': Icons.wine_bar_outlined, 'selected': false, 'image': 'https://images.unsplash.com/photo-1506377247377-2a5b3b417ebb?w=400&h=300&fit=crop'},
    {'title': 'Wellness & Spa', 'icon': Icons.spa_outlined, 'selected': true, 'image': 'https://images.unsplash.com/photo-1540555700478-4be289fbecef?w=400&h=300&fit=crop'},
    {'title': 'Wildlife Safari', 'icon': Icons.pets_outlined, 'selected': false, 'image': 'https://images.unsplash.com/photo-1516426122078-c23e76319801?w=400&h=300&fit=crop'},
    {'title': 'City Breaks', 'icon': Icons.map_outlined, 'selected': false, 'image': 'https://images.unsplash.com/photo-1449844908441-8829872d2607?w=400&h=300&fit=crop'},
    {'title': 'Mountain Treks', 'icon': Icons.landscape_outlined, 'selected': false, 'image': 'https://images.unsplash.com/photo-1464822759023-fed622ff2c3b?w=400&h=300&fit=crop'},
  ];

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: SafeArea(
        child: Column(
          children: [
            Expanded(
              child: SingleChildScrollView(
                padding: const EdgeInsets.symmetric(horizontal: 24.0, vertical: 32.0),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      'Style Your Escape',
                      style: Theme.of(context).textTheme.displayLarge,
                    ),
                    const SizedBox(height: 12),
                    Text(
                      'Select your travel interests to feed our AI engine for highly personalized matches.',
                      style: Theme.of(context).textTheme.bodyMedium?.copyWith(
                        fontSize: 16,
                      ),
                    ),
                    const SizedBox(height: 32),
                    
                    GridView.builder(
                      shrinkWrap: true,
                      physics: const NeverScrollableScrollPhysics(),
                      gridDelegate: const SliverGridDelegateWithFixedCrossAxisCount(
                        crossAxisCount: 2,
                        childAspectRatio: 1.4,
                        crossAxisSpacing: 16,
                        mainAxisSpacing: 16,
                      ),
                      itemCount: _preferences.length,
                      itemBuilder: (context, index) {
                        final pref = _preferences[index];
                        return _buildPreferenceCard(
                          title: pref['title'],
                          icon: pref['icon'],
                          imageUrl: pref['image'],
                          isSelected: pref['selected'],
                          onTap: () {
                            setState(() {
                              pref['selected'] = !pref['selected'];
                            });
                          },
                        );
                      },
                    ),
                  ],
                ),
              ),
            ),
            
            // Bottom Continue Bar
            Container(
              padding: const EdgeInsets.all(24.0),
              decoration: BoxDecoration(
                color: Colors.white,
                border: Border(
                  top: BorderSide(color: AppTheme.borderLight.withOpacity(0.5)),
                ),
              ),
              child: GradientButton(
                text: 'CONTINUE',
                onPressed: () {
                  Navigator.pop(context); // Go back or next step
                },
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildPreferenceCard({
    required String title,
    required IconData icon,
    required String imageUrl,
    required bool isSelected,
    required VoidCallback onTap,
  }) {
    return GestureDetector(
      onTap: onTap,
      child: Container(
        decoration: BoxDecoration(
          borderRadius: BorderRadius.circular(16),
          image: DecorationImage(
            image: NetworkImage(imageUrl),
            fit: BoxFit.cover,
            colorFilter: ColorFilter.mode(
              AppTheme.primaryDark.withOpacity(0.6), 
              BlendMode.darken,
            ),
          ),
        ),
        child: Stack(
          children: [
            Padding(
              padding: const EdgeInsets.all(12.0),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  Icon(icon, color: Colors.white, size: 20),
                  Text(
                    title,
                    style: const TextStyle(
                      color: Colors.white,
                      fontWeight: FontWeight.bold,
                      fontSize: 13,
                    ),
                  ),
                ],
              ),
            ),
            if (isSelected)
              Positioned(
                top: 12,
                right: 12,
                child: Container(
                  padding: const EdgeInsets.all(2),
                  decoration: const BoxDecoration(
                    color: AppTheme.accentCopper,
                    shape: BoxShape.circle,
                  ),
                  child: const Icon(Icons.check, color: Colors.white, size: 14),
                ),
              ),
          ],
        ),
      ),
    );
  }
}
