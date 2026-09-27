import 'package:flutter/material.dart';
import 'package:dio/dio.dart';
import 'package:firebase_auth/firebase_auth.dart';
import '../theme/app_theme.dart';
import '../widgets/gradient_button.dart';
import 'home_screen.dart';

class PreferenceScreen extends StatefulWidget {
  const PreferenceScreen({super.key});

  @override
  State<PreferenceScreen> createState() => _PreferenceScreenState();
}

class _PreferenceScreenState extends State<PreferenceScreen> {
  bool _isSaving = false;
  bool _isLoadingPrefs = true;

  @override
  void initState() {
    super.initState();
    _loadPreferences();
  }

  Future<void> _loadPreferences() async {
    final user = FirebaseAuth.instance.currentUser;
    if (user == null || user.email == null) {
      if (mounted) setState(() => _isLoadingPrefs = false);
      return;
    }
    try {
      final response = await Dio().get(
        'http://10.0.2.2:5085/api/auth/user/preferences',
        queryParameters: {'email': user.email},
      );
      if (response.statusCode == 200 && mounted) {
        final List<dynamic> prefs = response.data;
        setState(() {
          for (var item in _preferences) {
            item['selected'] = prefs.contains(item['title']);
          }
          _isLoadingPrefs = false;
        });
      }
    } catch (e) {
      debugPrint('Failed to load prefs: $e');
      if (mounted) setState(() => _isLoadingPrefs = false);
    }
  }
  final List<Map<String, dynamic>> _preferences = [
    {
      'title': 'Adventure',
      'icon': Icons.explore_outlined,
      'selected': false,
      'image':
          'https://images.unsplash.com/photo-1534447677768-be436bb09401?w=400&h=300&fit=crop',
    },
    {
      'title': 'Beach & Islands',
      'icon': Icons.wb_sunny_outlined,
      'selected': false,
      'image':
          'https://images.unsplash.com/photo-1507525428034-b723cf961d3e?w=400&h=300&fit=crop',
    },
    {
      'title': 'Cultural Heritage',
      'icon': Icons.account_balance_outlined,
      'selected': false,
      'image':
          'https://images.unsplash.com/photo-1548013146-72479768bada?w=400&h=300&fit=crop',
    },
    {
      'title': 'Food & Wine',
      'icon': Icons.wine_bar_outlined,
      'selected': false,
      'image':
          'https://images.unsplash.com/photo-1506377247377-2a5b3b417ebb?w=400&h=300&fit=crop',
    },
    {
      'title': 'Wellness & Spa',
      'icon': Icons.spa_outlined,
      'selected': false,
      'image':
          'https://images.unsplash.com/photo-1540555700478-4be289fbecef?w=400&h=300&fit=crop',
    },
    {
      'title': 'Wildlife Safari',
      'icon': Icons.pets_outlined,
      'selected': false,
      'image':
          'https://images.unsplash.com/photo-1516426122078-c23e76319801?w=400&h=300&fit=crop',
    },
    {
      'title': 'City Breaks',
      'icon': Icons.map_outlined,
      'selected': false,
      'image':
          'https://images.unsplash.com/photo-1449844908441-8829872d2607?w=400&h=300&fit=crop',
    },
    {
      'title': 'Mountain Treks',
      'icon': Icons.landscape_outlined,
      'selected': false,
      'image':
          'https://images.unsplash.com/photo-1464822759023-fed622ff2c3b?w=400&h=300&fit=crop',
    },
  ];

  Future<void> _savePreferences() async {
    final user = FirebaseAuth.instance.currentUser;
    if (user == null || user.email == null) return;

    setState(() {
      _isSaving = true;
    });

    final selectedPrefs = _preferences
        .where((p) => p['selected'] == true)
        .map((p) => p['title'] as String)
        .toList();

    try {
      final dio = Dio(
        BaseOptions(
          connectTimeout: const Duration(seconds: 5),
          receiveTimeout: const Duration(seconds: 5),
        ),
      );
      final response = await dio.put(
        'http://10.0.2.2:5085/api/auth/user/preferences',
        data: {'email': user.email, 'preferences': selectedPrefs},
      );

      if (response.statusCode == 200 && mounted) {
        // If we came from signup, navigate to HomeScreen. Otherwise, pop.
        if (Navigator.canPop(context)) {
          Navigator.pop(context, true);
        } else {
          Navigator.pushReplacement(
            context,
            MaterialPageRoute(builder: (context) => const HomeScreen()),
          );
        }
      }
    } catch (e) {
      debugPrint('Error saving preferences: $e');
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Failed to save preferences.')),
        );
      }
    } finally {
      if (mounted) {
        setState(() {
          _isSaving = false;
        });
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    if (_isLoadingPrefs) {
      return const Scaffold(
        backgroundColor: Colors.white,
        body: Center(child: CircularProgressIndicator()),
      );
    }

    return Scaffold(
      appBar: AppBar(
        backgroundColor: Colors.transparent,
        elevation: 0,
        leading: IconButton(
          icon: const Icon(Icons.arrow_back, color: AppTheme.primaryDark),
          onPressed: () {
            // Only pop if we can, otherwise we might be at the root of a flow
            if (Navigator.canPop(context)) {
              Navigator.pop(context);
            } else {
              Navigator.pushReplacement(
                context,
                MaterialPageRoute(builder: (context) => const HomeScreen()),
              );
            }
          },
        ),
      ),
      body: SafeArea(
        child: Column(
          children: [
            Expanded(
              child: SingleChildScrollView(
                padding: const EdgeInsets.symmetric(
                  horizontal: 24.0,
                  vertical: 32.0,
                ),
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
                      style: Theme.of(
                        context,
                      ).textTheme.bodyMedium?.copyWith(fontSize: 16),
                    ),
                    const SizedBox(height: 32),

                    GridView.builder(
                      shrinkWrap: true,
                      physics: const NeverScrollableScrollPhysics(),
                      gridDelegate:
                          const SliverGridDelegateWithFixedCrossAxisCount(
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
                  top: BorderSide(
                    color: AppTheme.borderLight.withValues(alpha: 0.5),
                  ),
                ),
              ),
              child: _isSaving
                  ? const Center(
                      child: CircularProgressIndicator(
                        color: AppTheme.accentCopper,
                      ),
                    )
                  : GradientButton(
                      text: 'CONTINUE',
                      onPressed: _savePreferences,
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
