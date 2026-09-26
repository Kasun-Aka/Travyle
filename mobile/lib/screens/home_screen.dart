import 'package:flutter/material.dart';
import 'package:firebase_auth/firebase_auth.dart';
import 'package:dio/dio.dart';
import '../theme/app_theme.dart';
import 'preference_screen.dart';
import 'destination_detail_screen.dart';
import 'login_screen.dart';
import 'destinations_list_screen.dart';
import 'ai_recommendation_screen.dart';

import 'profile_screen.dart';
import 'profile_notifications_screen.dart';

class HomeScreen extends StatefulWidget {
  const HomeScreen({super.key});

  @override
  State<HomeScreen> createState() => _HomeScreenState();
}

class _HomeScreenState extends State<HomeScreen> {
  int _currentIndex = 0;
  bool _isLoadingDestinations = true;
  List<dynamic> _destinations = [];
  int _unreadNotifications = 0;
  List<dynamic> _adminRecommendations = [];

  @override
  void initState() {
    super.initState();
    _fetchDestinations();
    _fetchNotifications();
  }

  Future<void> _fetchNotifications() async {
    final email = FirebaseAuth.instance.currentUser?.email;
    if (email == null) return;
    try {
      final response = await Dio().get(
        'http://10.0.2.2:5085/api/notifications/traveler/$email',
      );
      if (response.statusCode == 200) {
        final List notifs = response.data;
        final unread = notifs.where((n) => n['isRead'] == false).length;
        if (mounted) {
          setState(() {
            _unreadNotifications = unread;
            _adminRecommendations = notifs;
          });
        }
      }
    } catch (e) {
      debugPrint('Failed to load notifications: $e');
    }
  }

  Future<void> _fetchDestinations() async {
    try {
      final dio = Dio(
        BaseOptions(
          connectTimeout: const Duration(seconds: 5),
          receiveTimeout: const Duration(seconds: 5),
        ),
      );
      final response = await dio.get(
        'http://10.0.2.2:5085/api/Destinations?pageSize=5',
      );
      if (response.statusCode == 200 && response.data != null) {
        if (mounted) {
          setState(() {
            _destinations = response.data['items'] ?? [];
            _isLoadingDestinations = false;
          });
        }
      }
    } catch (e) {
      debugPrint('Error fetching destinations: $e');
      if (mounted) {
        setState(() {
          _isLoadingDestinations = false;
        });
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: SafeArea(
        child: IndexedStack(
          index: _currentIndex,
          children: [
            _buildHomeContent(context),
            const Center(child: Text('Bookings - Coming Soon')),
            const Center(child: Text('Guide - Coming Soon')),
            const Center(child: Text('Support - Coming Soon')),
            const ProfileScreen(),
          ],
        ),
      ),
      bottomNavigationBar: BottomNavigationBar(
        currentIndex: _currentIndex,
        onTap: (index) {
          setState(() {
            _currentIndex = index;
          });
        },
        type: BottomNavigationBarType.fixed,
        selectedItemColor: AppTheme.primaryDark,
        unselectedItemColor: AppTheme.textGrey,
        showUnselectedLabels: true,
        selectedLabelStyle: const TextStyle(
          fontWeight: FontWeight.bold,
          fontSize: 10,
        ),
        unselectedLabelStyle: const TextStyle(fontSize: 10),
        items: const [
          BottomNavigationBarItem(icon: Icon(Icons.explore), label: 'Explore'),
          BottomNavigationBarItem(
            icon: Icon(Icons.calendar_today),
            label: 'Bookings',
          ),
          BottomNavigationBarItem(icon: Icon(Icons.map), label: 'Guide'),
          BottomNavigationBarItem(
            icon: Icon(Icons.help_outline),
            label: 'Support',
          ),
          BottomNavigationBarItem(
            icon: Icon(Icons.person_outline),
            label: 'Profile',
          ),
        ],
      ),
    );
  }

  Future<void> _onRefresh() async {
    await _fetchNotifications();
    await _fetchDestinations();
  }

  Widget _buildHomeContent(BuildContext context) {
    return RefreshIndicator(
      onRefresh: _onRefresh,
      color: AppTheme.accentCopper,
      child: SingleChildScrollView(
      padding: const EdgeInsets.symmetric(horizontal: 24.0, vertical: 20.0),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          // Header
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  const Text(
                    'WELCOME BACK',
                    style: TextStyle(
                      color: AppTheme.textGrey,
                      fontSize: 12,
                      fontWeight: FontWeight.w600,
                      letterSpacing: 1.2,
                    ),
                  ),
                  const SizedBox(height: 4),
                  Text(
                    FirebaseAuth.instance.currentUser?.displayName ??
                        'Alexander',
                    style: Theme.of(
                      context,
                    ).textTheme.displayLarge?.copyWith(fontSize: 28),
                  ),
                ],
              ),
              Row(
                children: [
                  Stack(
                    alignment: Alignment.center,
                    children: [
                      IconButton(
                        icon: const Icon(Icons.notifications_none, size: 28, color: AppTheme.textDark),
                        onPressed: () async {
                          await Navigator.push(
                            context,
                            MaterialPageRoute(builder: (context) => const ProfileNotificationsScreen()),
                          );
                          _fetchNotifications(); // Refresh on return
                        },
                      ),
                      if (_unreadNotifications > 0)
                        Positioned(
                          right: 8,
                          top: 8,
                          child: Container(
                            padding: const EdgeInsets.all(4),
                            decoration: const BoxDecoration(
                              color: Colors.red,
                              shape: BoxShape.circle,
                            ),
                            child: Text(
                              '$_unreadNotifications',
                              style: const TextStyle(
                                color: Colors.white,
                                fontSize: 10,
                                fontWeight: FontWeight.bold,
                              ),
                            ),
                          ),
                        ),
                    ],
                  ),
                  const SizedBox(width: 8),
                  GestureDetector(
                    onTap: () {
                      setState(() {
                        _currentIndex = 4; // Switch to Profile Tab
                      });
                    },
                    child: CircleAvatar(
                      radius: 24,
                      backgroundImage: NetworkImage(
                        FirebaseAuth.instance.currentUser?.photoURL ??
                            'https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?w=200&h=200&fit=crop',
                      ),
                    ),
                  ),
                ],
              ),
            ],
          ),
          const SizedBox(height: 24),

          // Search Bar
          TextField(
            decoration: InputDecoration(
              hintText: 'Search global curated tours...',
              prefixIcon: const Icon(Icons.search, color: AppTheme.textGrey),
              contentPadding: const EdgeInsets.symmetric(vertical: 16),
              border: OutlineInputBorder(
                borderRadius: BorderRadius.circular(30),
                borderSide: const BorderSide(color: AppTheme.borderLight),
              ),
            ),
          ),
          const SizedBox(height: 32),

          // AI Match Banner
          Container(
            width: double.infinity,
            padding: const EdgeInsets.all(20),
            decoration: BoxDecoration(
              gradient: const LinearGradient(
                colors: [AppTheme.primaryDark, AppTheme.accentCopper],
                begin: Alignment.topLeft,
                end: Alignment.bottomRight,
              ),
              borderRadius: BorderRadius.circular(20),
              boxShadow: [
                BoxShadow(
                  color: AppTheme.accentCopper.withValues(alpha: 0.3),
                  blurRadius: 10,
                  offset: const Offset(0, 4),
                ),
              ],
            ),
            child: Row(
              children: [
                const Icon(Icons.auto_awesome, color: Colors.white, size: 40),
                const SizedBox(width: 16),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: const [
                      Text(
                        'Not sure where to go?',
                        style: TextStyle(
                          color: Colors.white,
                          fontSize: 16,
                          fontWeight: FontWeight.bold,
                        ),
                      ),
                      SizedBox(height: 4),
                      Text(
                        'Let Gemini find your perfect match',
                        style: TextStyle(
                          color: Colors.white70,
                          fontSize: 12,
                        ),
                      ),
                    ],
                  ),
                ),
                ElevatedButton(
                  onPressed: () {
                    Navigator.push(
                      context,
                      MaterialPageRoute(
                        builder: (context) => const AiRecommendationScreen(),
                      ),
                    );
                  },
                  style: ElevatedButton.styleFrom(
                    backgroundColor: Colors.white,
                    foregroundColor: AppTheme.primaryDark,
                    shape: RoundedRectangleBorder(
                      borderRadius: BorderRadius.circular(20),
                    ),
                  ),
                  child: const Text('Try Now', style: TextStyle(fontWeight: FontWeight.bold)),
                ),
              ],
            ),
          ),
          const SizedBox(height: 32),

          // Popular Destinations Header
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Text(
                'Popular Destinations',
                style: TextStyle(
                  color: AppTheme.primaryDark,
                  fontSize: 20,
                  fontWeight: FontWeight.bold,
                ),
              ),
              TextButton(
                onPressed: () {
                  Navigator.push(
                    context,
                    MaterialPageRoute(
                      builder: (context) => const DestinationsListScreen(),
                    ),
                  );
                },
                child: const Text(
                  'View All',
                  style: TextStyle(color: AppTheme.accentCopper),
                ),
              ),
            ],
          ),
          const SizedBox(height: 16),

          // Horizontal Scroll List (Destinations)
          SizedBox(
            height: 220,
            child: _isLoadingDestinations
                ? const Center(child: CircularProgressIndicator())
                : _destinations.isEmpty
                ? const Center(child: Text('No destinations found.'))
                : ListView.builder(
                    scrollDirection: Axis.horizontal,
                    itemCount: _destinations.length,
                    itemBuilder: (context, index) {
                      final dest = _destinations[index];
                      return Padding(
                        padding: const EdgeInsets.only(right: 16.0),
                        child: _buildDestinationCard(context, dest),
                      );
                    },
                  ),
          ),
          const SizedBox(height: 32),

          // Recommended For You Header
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Text(
                'Recommended For You',
                style: TextStyle(
                  color: AppTheme.primaryDark,
                  fontSize: 20,
                  fontWeight: FontWeight.bold,
                ),
              ),
              Container(
                padding: const EdgeInsets.symmetric(
                  horizontal: 10,
                  vertical: 4,
                ),
                decoration: BoxDecoration(
                  color: AppTheme.accentCopper.withOpacity(0.1),
                  borderRadius: BorderRadius.circular(12),
                ),
                child: const Text(
                  'AI MATCH',
                  style: TextStyle(
                    color: AppTheme.accentCopper,
                    fontSize: 10,
                    fontWeight: FontWeight.bold,
                    letterSpacing: 1.0,
                  ),
                ),
              ),
            ],
          ),
          const SizedBox(height: 16),

          // AI Recommended Cards
          ListView.builder(
            shrinkWrap: true,
            physics: const NeverScrollableScrollPhysics(),
            itemCount: _adminRecommendations.length,
            itemBuilder: (context, index) {
              final notif = _adminRecommendations[index];
              final dest = notif["destination"] ?? {};
              return Padding(
                padding: const EdgeInsets.only(bottom: 24.0),
                child: _buildAdminRecommendedCard(context, notif, dest),
              );
            },
          ),
          
          if (_adminRecommendations.isNotEmpty)
            const Padding(
              padding: EdgeInsets.symmetric(vertical: 8.0),
              child: Divider(color: AppTheme.borderLight),
            ),
            
          _buildRecommendedCard(
            context,
            'Mediterranean Wellness Retreat',
            'Sardinia',
            '\$4,250',
            '8 Days',
            'https://images.unsplash.com/photo-1510414842594-a61c69b5ae57?w=800&h=400&fit=crop',
          ),
          const SizedBox(height: 24),
          _buildRecommendedCard(
            context,
            'Cultural & Culinary Silk Road',
            'Samarkand',
            '\$6,100',
            '12 Days',
            'https://images.unsplash.com/photo-1542640244-76abc29e4368?w=800&h=400&fit=crop',
          ),
          const SizedBox(height: 40),
        ],
      ),
    ),
    );
  }

  Widget _buildAdminRecommendedCard(BuildContext context, dynamic notif, dynamic dest) {
    return Container(
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(20),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withOpacity(0.05),
            blurRadius: 15,
            offset: const Offset(0, 5),
          ),
        ],
        border: Border.all(
          color: (notif["isRead"] == true) ? Colors.transparent : AppTheme.primaryDark.withOpacity(0.3),
          width: 2,
        ),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          ClipRRect(
            borderRadius: const BorderRadius.vertical(top: Radius.circular(20)),
            child: Stack(
              children: [
                Image.network(
                  dest["imageUrl"] ?? 'https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?w=800&h=400&fit=crop',
                  height: 160,
                  width: double.infinity,
                  fit: BoxFit.cover,
                ),
                if (notif["isRead"] == false)
                  Positioned(
                    top: 12,
                    left: 12,
                    child: Container(
                      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
                      decoration: BoxDecoration(
                        color: Colors.red,
                        borderRadius: BorderRadius.circular(12),
                      ),
                      child: const Text(
                        'NEW MATCH',
                        style: TextStyle(
                          color: Colors.white,
                          fontSize: 10,
                          fontWeight: FontWeight.bold,
                        ),
                      ),
                    ),
                  ),
              ],
            ),
          ),
          Padding(
            padding: const EdgeInsets.all(20),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    Expanded(
                      child: Text(
                        dest["name"] ?? 'Recommended Destination',
                        style: const TextStyle(
                          fontSize: 18,
                          fontWeight: FontWeight.bold,
                          color: AppTheme.textDark,
                        ),
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: 4),
                Row(
                  children: [
                    const Icon(Icons.location_on, size: 14, color: AppTheme.textGrey),
                    const SizedBox(width: 4),
                    Text(
                      dest["region"] ?? 'Global',
                      style: const TextStyle(color: AppTheme.textGrey, fontSize: 12),
                    ),
                  ],
                ),
                const SizedBox(height: 16),
                Container(
                  padding: const EdgeInsets.all(12),
                  decoration: BoxDecoration(
                    color: AppTheme.backgroundLight,
                    borderRadius: BorderRadius.circular(12),
                    border: Border.all(color: AppTheme.borderLight),
                  ),
                  child: Row(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      const Icon(Icons.auto_awesome, size: 16, color: AppTheme.accentCopper),
                      const SizedBox(width: 8),
                      Expanded(
                        child: Text(
                          '"${notif["pitch"] ?? ""}"',
                          style: const TextStyle(
                            fontSize: 12,
                            fontStyle: FontStyle.italic,
                            color: AppTheme.textGrey,
                            height: 1.4,
                          ),
                        ),
                      ),
                    ],
                  ),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildDestinationCard(
    BuildContext context,
    Map<String, dynamic> dest,
  ) {
    final title = dest['name'] ?? 'Unknown';
    final rating = (dest['averageRating'] ?? 0.0).toStringAsFixed(1);
    final imageUrl =
        dest['imageUrl'] ??
        'https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?w=400&h=300&fit=crop';

    return GestureDetector(
      onTap: () {
        Navigator.push(
          context,
          MaterialPageRoute(
            builder: (context) => DestinationDetailScreen(destination: dest),
          ),
        );
      },
      child: Container(
        width: 160,
        decoration: BoxDecoration(
          color: Colors.white,
          borderRadius: BorderRadius.circular(16),
          boxShadow: [
            BoxShadow(
              color: Colors.black.withOpacity(0.04),
              blurRadius: 10,
              offset: const Offset(0, 4),
            ),
          ],
        ),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            ClipRRect(
              borderRadius: const BorderRadius.only(
                topLeft: Radius.circular(16),
                topRight: Radius.circular(16),
              ),
              child: Image.network(
                imageUrl,
                height: 140,
                width: double.infinity,
                fit: BoxFit.cover,
              ),
            ),
            Padding(
              padding: const EdgeInsets.all(12.0),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    title,
                    style: const TextStyle(
                      fontWeight: FontWeight.bold,
                      fontSize: 14,
                      color: AppTheme.textDark,
                    ),
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                  ),
                  const SizedBox(height: 4),
                  Row(
                    children: [
                      const Icon(Icons.star, color: Colors.amber, size: 14),
                      const SizedBox(width: 4),
                      Text(
                        rating,
                        style: const TextStyle(
                          color: AppTheme.textGrey,
                          fontSize: 12,
                          fontWeight: FontWeight.w600,
                        ),
                      ),
                    ],
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildRecommendedCard(
    BuildContext context,
    String title,
    String location,
    String price,
    String duration,
    String imageUrl,
  ) {
    return Container(
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(20),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withOpacity(0.05),
            blurRadius: 15,
            offset: const Offset(0, 8),
          ),
        ],
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          // Image Header
          Stack(
            children: [
              ClipRRect(
                borderRadius: const BorderRadius.only(
                  topLeft: Radius.circular(20),
                  topRight: Radius.circular(20),
                ),
                child: Image.network(
                  imageUrl,
                  height: 180,
                  width: double.infinity,
                  fit: BoxFit.cover,
                ),
              ),
              Positioned(
                top: 16,
                right: 16,
                child: Container(
                  padding: const EdgeInsets.all(8),
                  decoration: const BoxDecoration(
                    color: Colors.white,
                    shape: BoxShape.circle,
                  ),
                  child: const Icon(
                    Icons.favorite_border,
                    color: AppTheme.accentCopper,
                    size: 20,
                  ),
                ),
              ),
              Positioned(
                bottom: 16,
                left: 16,
                child: Container(
                  padding: const EdgeInsets.symmetric(
                    horizontal: 12,
                    vertical: 6,
                  ),
                  decoration: BoxDecoration(
                    color: Colors.white,
                    borderRadius: BorderRadius.circular(20),
                  ),
                  child: Text(
                    duration,
                    style: const TextStyle(
                      fontWeight: FontWeight.bold,
                      fontSize: 12,
                      color: AppTheme.primaryDark,
                    ),
                  ),
                ),
              ),
            ],
          ),
          // Content Below Image
          Padding(
            padding: const EdgeInsets.all(20.0),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  title,
                  style: const TextStyle(
                    fontSize: 16,
                    fontWeight: FontWeight.bold,
                    color: AppTheme.primaryDark,
                  ),
                ),
                const SizedBox(height: 8),
                Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    Text(
                      location,
                      style: const TextStyle(
                        color: AppTheme.textGrey,
                        fontSize: 14,
                      ),
                    ),
                    Text(
                      price,
                      style: const TextStyle(
                        fontSize: 18,
                        fontWeight: FontWeight.bold,
                        color: AppTheme.primaryDark,
                      ),
                    ),
                  ],
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}
