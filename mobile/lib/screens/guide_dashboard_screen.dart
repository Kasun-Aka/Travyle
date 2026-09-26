import 'package:flutter/material.dart';
import 'package:flutter/foundation.dart';
import 'package:dio/dio.dart';
import 'package:firebase_auth/firebase_auth.dart';
import 'package:mobile/screens/live_tour_activity_screen.dart';
import 'package:mobile/screens/qr_checkin_screen.dart';

class GuideDashboardScreen extends StatefulWidget {
  const GuideDashboardScreen({super.key});

  @override
  State<GuideDashboardScreen> createState() => _GuideDashboardScreenState();
}

class _GuideDashboardScreenState extends State<GuideDashboardScreen> {
  Map<String, dynamic>? _dashboardData;
  bool _isLoading = true;

  @override
  void initState() {
    super.initState();
    _fetchDashboardData();
  }

  Future<void> _fetchDashboardData() async {
    try {
      final user = FirebaseAuth.instance.currentUser;
      if (user == null) return;
      
      final dio = Dio();
      final baseUrl = kIsWeb ? 'http://localhost:5085' : 'http://10.0.2.2:5085';
      final response = await dio.get(
        '$baseUrl/api/operations/dashboard',
        queryParameters: {'email': user.email},
      );

      if (mounted) {
        setState(() {
          _dashboardData = response.data;
          _isLoading = false;
        });
      }
    } catch (e) {
      debugPrint('Failed to load dashboard: $e');
      if (mounted) {
        setState(() {
          _isLoading = false;
        });
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    if (_isLoading) {
      return const Scaffold(
        body: Center(child: CircularProgressIndicator()),
      );
    }

    if (_dashboardData == null) {
      return const Scaffold(
        body: Center(child: Text('Failed to load data.')),
      );
    }

    final headerName = _dashboardData!['headerName'];
    final headerTitle = _dashboardData!['headerTitle'];
    final stats = _dashboardData!['stats'] as List<dynamic>;
    final tours = _dashboardData!['tours'] as List<dynamic>;
    final isGuide = _dashboardData!['role'] == "Local Guide" || _dashboardData!['role'] == "Tour Operator";

    return Scaffold(
      backgroundColor: const Color(0xFFF8F9FA),
      body: SafeArea(
        child: SingleChildScrollView(
          padding: const EdgeInsets.symmetric(horizontal: 20.0, vertical: 24.0),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              _buildHeader(context, headerName, headerTitle, isGuide),
              const SizedBox(height: 24.0),
              _buildStatsRow(stats),
              const SizedBox(height: 32.0),
              Text(
                isGuide ? 'Today\'s Assigned Tours' : 'Your Upcoming Tours',
                style: const TextStyle(
                  fontSize: 18.0,
                  fontWeight: FontWeight.w700,
                  color: Color(0xFF133E4D),
                ),
              ),
              const SizedBox(height: 16.0),
              ...tours.map((t) {
                return Padding(
                  padding: const EdgeInsets.only(bottom: 16.0),
                  child: _buildTourCard(
                    time: t['time'],
                    travelers: t['travelers'],
                    title: t['title'],
                    location: t['location'],
                    actionText: t['actionType'] == 'CHECK_IN' 
                        ? (isGuide ? 'CHECK IN / SCAN QR' : 'VIEW QR CODE')
                        : (isGuide ? 'START TOUR' : 'VIEW LIVE TOUR'),
                    actionColor: t['actionType'] == 'CHECK_IN' ? const Color(0xFFDD8866) : const Color(0xFF133E4D),
                    onActionPressed: () {
                      if (t['actionType'] == 'CHECK_IN') {
                        // For Guide it is QR scanner (if built), for tourist it's QR display.
                        // Currently QRCheckinScreen can serve as a placeholder for both
                        Navigator.push(
                          context,
                          MaterialPageRoute(
                            builder: (context) => const QRCheckinScreen(),
                          ),
                        );
                      } else {
                        Navigator.push(
                          context,
                          MaterialPageRoute(
                            builder: (context) => LiveTourActivityScreen(isGuide: isGuide),
                          ),
                        );
                      }
                    },
                  ),
                );
              }),
            ],
          ),
        ),
      ),
    );
  }

  Widget _buildHeader(BuildContext context, String name, String title, bool isGuide) {
    return Row(
      children: [
        CircleAvatar(
          radius: 24.0,
          backgroundImage: NetworkImage(
              'https://ui-avatars.com/api/?name=${Uri.encodeComponent(name)}&background=DD8866&color=fff'),
        ),
        const SizedBox(width: 12.0),
        Expanded(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                title,
                style: const TextStyle(
                  fontSize: 12.0,
                  fontWeight: FontWeight.bold,
                  color: Colors.grey,
                  letterSpacing: 0.5,
                ),
              ),
              Text(
                name,
                style: const TextStyle(
                  fontSize: 20.0,
                  fontWeight: FontWeight.bold,
                  color: Color(0xFF133E4D),
                ),
              ),
            ],
          ),
        ),
        InkWell(
          onTap: () {
            Navigator.push(
              context,
              MaterialPageRoute(
                builder: (context) => const QRCheckinScreen(),
              ),
            );
          },
          borderRadius: BorderRadius.circular(24.0),
          child: Container(
            padding: const EdgeInsets.all(12.0),
            decoration: BoxDecoration(
              shape: BoxShape.circle,
              border: Border.all(color: Colors.grey.shade300),
            ),
            child: Icon(
              isGuide ? Icons.qr_code_scanner : Icons.qr_code,
              color: const Color(0xFF133E4D),
            ),
          ),
        ),
      ],
    );
  }

  Widget _buildStatsRow(List<dynamic> stats) {
    return Row(
      mainAxisAlignment: MainAxisAlignment.spaceBetween,
      children: stats.map((s) => _buildStatCard(s['value'].toString(), s['label'].toString())).toList(),
    );
  }

  Widget _buildStatCard(String value, String label) {
    return Expanded(
      child: Container(
        margin: const EdgeInsets.symmetric(horizontal: 4.0),
        padding: const EdgeInsets.symmetric(vertical: 16.0),
        decoration: BoxDecoration(
          color: Colors.white,
          borderRadius: BorderRadius.circular(16.0),
          border: Border.all(color: Colors.grey.shade200),
        ),
        child: Column(
          children: [
            Text(
              value,
              style: const TextStyle(
                fontSize: 24.0,
                fontWeight: FontWeight.w800,
                color: Color(0xFF133E4D),
              ),
            ),
            const SizedBox(height: 4.0),
            Text(
              label,
              style: const TextStyle(
                fontSize: 10.0,
                fontWeight: FontWeight.bold,
                color: Colors.grey,
                letterSpacing: 0.5,
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildTourCard({
    required String time,
    required String travelers,
    required String title,
    required String location,
    required String actionText,
    required Color actionColor,
    required VoidCallback onActionPressed,
  }) {
    return Container(
      padding: const EdgeInsets.all(16.0),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(20.0),
        border: Border.all(color: Colors.grey.shade200),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Container(
                padding:
                    const EdgeInsets.symmetric(horizontal: 10.0, vertical: 6.0),
                decoration: BoxDecoration(
                  color: const Color(0xFFE5EFF3),
                  borderRadius: BorderRadius.circular(8.0),
                ),
                child: Text(
                  time,
                  style: const TextStyle(
                    fontSize: 12.0,
                    fontWeight: FontWeight.bold,
                    color: Color(0xFF133E4D),
                  ),
                ),
              ),
              Text(
                travelers,
                style: const TextStyle(
                  fontSize: 13.0,
                  color: Colors.grey,
                ),
              ),
            ],
          ),
          const SizedBox(height: 16.0),
          Text(
            title,
            style: const TextStyle(
              fontSize: 18.0,
              fontWeight: FontWeight.w700,
              color: Color(0xFF133E4D),
            ),
          ),
          const SizedBox(height: 8.0),
          Row(
            children: [
              const Icon(Icons.location_on_outlined,
                  size: 16.0, color: Colors.grey),
              const SizedBox(width: 4.0),
              Text(
                location,
                style: const TextStyle(
                  fontSize: 13.0,
                  color: Colors.grey,
                ),
              ),
            ],
          ),
          const SizedBox(height: 20.0),
          const Divider(height: 1.0, color: Color(0xFFEEEEEE)),
          const SizedBox(height: 20.0),
          SizedBox(
            width: double.infinity,
            child: ElevatedButton(
              onPressed: onActionPressed,
              style: ElevatedButton.styleFrom(
                backgroundColor: actionColor,
                foregroundColor: Colors.white,
                padding: const EdgeInsets.symmetric(vertical: 16.0),
                shape: RoundedRectangleBorder(
                  borderRadius: BorderRadius.circular(24.0),
                ),
                elevation: 0,
              ),
              child: Text(
                actionText,
                style: const TextStyle(
                  fontSize: 14.0,
                  fontWeight: FontWeight.w700,
                  letterSpacing: 0.5,
                ),
              ),
            ),
          ),
        ],
      ),
    );
  }
}

