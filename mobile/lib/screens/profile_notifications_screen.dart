import 'package:flutter/material.dart';
import 'package:firebase_auth/firebase_auth.dart';
import 'package:dio/dio.dart';
import '../theme/app_theme.dart';

class ProfileNotificationsScreen extends StatefulWidget {
  const ProfileNotificationsScreen({super.key});

  @override
  State<ProfileNotificationsScreen> createState() => _ProfileNotificationsScreenState();
}

class _ProfileNotificationsScreenState extends State<ProfileNotificationsScreen> {
  bool _isLoading = true;
  List<dynamic> _notifications = [];

  @override
  void initState() {
    super.initState();
    _fetchNotifications();
  }

  Future<void> _fetchNotifications() async {
    final email = FirebaseAuth.instance.currentUser?.email;
    if (email == null) return;
    
    try {
      final response = await Dio().get(
        'http://10.0.2.2:5085/api/notifications/traveler/$email',
      );
      if (response.statusCode == 200 && mounted) {
        setState(() {
          _notifications = response.data;
          _isLoading = false;
        });
      }
    } catch (e) {
      debugPrint('Failed to fetch notifications: $e');
      if (mounted) {
        setState(() {
          _isLoading = false;
        });
      }
    }
  }

  Future<void> _markAsRead(String id) async {
    try {
      await Dio().put('http://10.0.2.2:5085/api/notifications/$id/read');
      setState(() {
        final index = _notifications.indexWhere((n) => n['id'] == id);
        if (index != -1) {
          _notifications[index]['isRead'] = true;
        }
      });
    } catch (e) {
      debugPrint('Failed to mark notification as read: $e');
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: Colors.white,
      appBar: AppBar(
        title: const Text(
          'Notifications',
          style: TextStyle(color: AppTheme.textDark, fontWeight: FontWeight.bold),
        ),
        backgroundColor: Colors.white,
        elevation: 0,
        iconTheme: const IconThemeData(color: AppTheme.textDark),
      ),
      body: _isLoading
          ? const Center(child: CircularProgressIndicator(color: AppTheme.primaryDark))
          : _notifications.isEmpty
              ? const Center(
                  child: Text(
                    'No notifications yet.',
                    style: TextStyle(color: AppTheme.textGrey, fontSize: 16),
                  ),
                )
              : ListView.builder(
                  padding: const EdgeInsets.all(16),
                  itemCount: _notifications.length,
                  itemBuilder: (context, index) {
                    final notif = _notifications[index];
                    final isRead = notif['isRead'] == true;
                    final dest = notif['destination'];

                    return GestureDetector(
                      onTap: () {
                        if (!isRead) _markAsRead(notif['id']);
                        // Optional: Navigate to destination detail screen
                      },
                      child: Container(
                        margin: const EdgeInsets.only(bottom: 16),
                        padding: const EdgeInsets.all(16),
                        decoration: BoxDecoration(
                          color: isRead ? Colors.white : AppTheme.backgroundLight.withOpacity(0.3),
                          borderRadius: BorderRadius.circular(16),
                          border: Border.all(
                            color: isRead ? AppTheme.borderLight : AppTheme.primaryDark.withOpacity(0.5),
                          ),
                          boxShadow: [
                            BoxShadow(
                              color: Colors.black.withOpacity(0.03),
                              blurRadius: 10,
                              offset: const Offset(0, 4),
                            ),
                          ],
                        ),
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Row(
                              children: [
                                const Icon(Icons.auto_awesome, color: AppTheme.primaryDark, size: 20),
                                const SizedBox(width: 8),
                                const Text(
                                  'AI Concierge Pitch',
                                  style: TextStyle(
                                    fontWeight: FontWeight.bold,
                                    fontSize: 14,
                                    color: AppTheme.primaryDark,
                                  ),
                                ),
                                const Spacer(),
                                if (!isRead)
                                  Container(
                                    padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
                                    decoration: BoxDecoration(
                                      color: Colors.red,
                                      borderRadius: BorderRadius.circular(10),
                                    ),
                                    child: const Text(
                                      'NEW',
                                      style: TextStyle(color: Colors.white, fontSize: 10, fontWeight: FontWeight.bold),
                                    ),
                                  ),
                              ],
                            ),
                            const SizedBox(height: 12),
                            Text(
                              dest != null ? 'Recommended: ${dest['name']} (${dest['region']})' : 'Recommended Destination',
                              style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 16, color: AppTheme.textDark),
                            ),
                            const SizedBox(height: 8),
                            Text(
                              '"${notif['pitch']}"',
                              style: const TextStyle(fontSize: 14, color: AppTheme.textGrey, fontStyle: FontStyle.italic),
                            ),
                            const SizedBox(height: 12),
                            Text(
                              DateTime.parse(notif['sentAt']).toLocal().toString().split('.')[0],
                              style: const TextStyle(fontSize: 12, color: AppTheme.textGrey),
                            ),
                          ],
                        ),
                      ),
                    );
                  },
                ),
    );
  }
}
