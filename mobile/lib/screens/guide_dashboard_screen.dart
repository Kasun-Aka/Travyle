import 'package:flutter/material.dart';
import 'package:flutter/foundation.dart';
import 'package:dio/dio.dart';
import 'package:firebase_auth/firebase_auth.dart';
import 'package:mobile/screens/live_tour_activity_screen.dart';
import 'package:mobile/screens/qr_checkin_screen.dart';
import '../services/api_config.dart';

class GuideDashboardScreen extends StatefulWidget {
  const GuideDashboardScreen({super.key});

  @override
  State<GuideDashboardScreen> createState() => _GuideDashboardScreenState();
}

class _GuideDashboardScreenState extends State<GuideDashboardScreen> {
  Map<String, dynamic>? _dashboardData;
  bool _isLoading = true;
  String? _errorMessage;

  @override
  void initState() {
    super.initState();
    _fetchDashboardData();
  }

  Future<void> _fetchDashboardData() async {
    try {
      final user = FirebaseAuth.instance.currentUser;
      if (user == null) {
        debugPrint('Dashboard: No authenticated user found');
        if (mounted) setState(() { _isLoading = false; _errorMessage = 'Not signed in'; });
        return;
      }
      
      final dio = Dio();
      final baseUrl = ApiConfig.baseUrl;
      final idToken = await user.getIdToken();
      debugPrint('Dashboard: Fetching from $baseUrl/api/operations/dashboard?email=${user.email}');
      final response = await dio.get(
        '$baseUrl/api/operations/dashboard',
        queryParameters: {'email': user.email},
        options: Options(headers: {'Authorization': 'Bearer $idToken'}),
      );

      if (mounted) {
        final data = response.data;
        if (data is Map<String, dynamic>) {
          setState(() {
            _dashboardData = data;
            _isLoading = false;
          });
        } else if (data is Map) {
          setState(() {
            _dashboardData = Map<String, dynamic>.from(data);
            _isLoading = false;
          });
        } else {
          debugPrint('Dashboard: Unexpected response type: ${data.runtimeType}');
          setState(() {
            _isLoading = false;
            _errorMessage = 'Unexpected response format';
          });
        }
      }
    } on DioException catch (e) {
      debugPrint('Dashboard DioError: ${e.type} - ${e.message}');
      debugPrint('Dashboard response status: ${e.response?.statusCode}');
      debugPrint('Dashboard response data: ${e.response?.data}');
      if (mounted) {
        String errorMsg;
        if (e.response?.statusCode == 404) {
          errorMsg = 'User not found in backend database';
        } else if (e.response?.statusCode == 500) {
          final data = e.response?.data;
          if (data is Map && data['details'] != null) {
            errorMsg = 'Server error: ${data['details']}';
          } else {
            errorMsg = 'Internal server error (500)';
          }
        } else {
          errorMsg = 'Connection error: ${e.message}';
        }
        setState(() {
          _isLoading = false;
          _errorMessage = errorMsg;
        });
      }
    } catch (e) {
      debugPrint('Dashboard error: $e');
      if (mounted) {
        setState(() {
          _isLoading = false;
          _errorMessage = e.toString();
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
      return Scaffold(
        body: Center(
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              const Icon(Icons.error_outline, size: 48, color: Colors.grey),
              const SizedBox(height: 12),
              Text(
                _errorMessage ?? 'Failed to load data.',
                textAlign: TextAlign.center,
                style: const TextStyle(fontSize: 14, color: Colors.grey),
              ),
              const SizedBox(height: 16),
              ElevatedButton.icon(
                onPressed: () {
                  setState(() { _isLoading = true; _errorMessage = null; });
                  _fetchDashboardData();
                },
                icon: const Icon(Icons.refresh),
                label: const Text('Retry'),
              ),
            ],
          ),
        ),
      );
    }

    final headerName = _dashboardData!['headerName'] ?? 'Guide';
    final headerTitle = _dashboardData!['headerTitle'] ?? 'LOCAL GUIDE';
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
                    isGuide: isGuide,
                    onQrPressed: () {
                      Navigator.push(
                        context,
                        MaterialPageRoute(
                          builder: (context) => const QRCheckinScreen(),
                        ),
                      );
                    },
                    onLiveTourPressed: () {
                      Navigator.push(
                        context,
                        MaterialPageRoute(
                          builder: (context) => LiveTourActivityScreen(isGuide: isGuide),
                        ),
                      );
                    },
                  ),
                );
              }),
              if (tours.isEmpty)
                Container(
                  padding: const EdgeInsets.symmetric(vertical: 40.0),
                  alignment: Alignment.center,
                  child: Column(
                    children: const [
                      Icon(Icons.explore_off, size: 48, color: Colors.grey),
                      SizedBox(height: 12),
                      Text(
                        'No tours scheduled yet.',
                        style: TextStyle(fontSize: 16, color: Colors.grey),
                      ),
                    ],
                  ),
                ),
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
    required bool isGuide,
    required VoidCallback onQrPressed,
    required VoidCallback onLiveTourPressed,
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
              Expanded(
                child: Text(
                  location,
                  style: const TextStyle(
                    fontSize: 13.0,
                    color: Colors.grey,
                  ),
                  overflow: TextOverflow.ellipsis,
                ),
              ),
            ],
          ),
          const SizedBox(height: 20.0),
          const Divider(height: 1.0, color: Color(0xFFEEEEEE)),
          const SizedBox(height: 16.0),
          Row(
            children: [
              Expanded(
                child: OutlinedButton.icon(
                  onPressed: onQrPressed,
                  icon: Icon(
                    isGuide ? Icons.qr_code_scanner : Icons.qr_code,
                    size: 18,
                    color: const Color(0xFFDD8866),
                  ),
                  label: Text(
                    isGuide ? 'SCAN QR' : 'VIEW QR',
                    style: const TextStyle(
                      fontSize: 12.0,
                      fontWeight: FontWeight.w700,
                      color: Color(0xFFDD8866),
                      letterSpacing: 0.5,
                    ),
                  ),
                  style: OutlinedButton.styleFrom(
                    padding: const EdgeInsets.symmetric(vertical: 14.0),
                    side: const BorderSide(color: Color(0xFFDD8866)),
                    shape: RoundedRectangleBorder(
                      borderRadius: BorderRadius.circular(24.0),
                    ),
                  ),
                ),
              ),
              const SizedBox(width: 12.0),
              Expanded(
                child: ElevatedButton.icon(
                  onPressed: onLiveTourPressed,
                  icon: const Icon(Icons.explore, size: 18, color: Colors.white),
                  label: const Text(
                    'LIVE TOUR',
                    style: TextStyle(
                      fontSize: 12.0,
                      fontWeight: FontWeight.w700,
                      letterSpacing: 0.5,
                    ),
                  ),
                  style: ElevatedButton.styleFrom(
                    backgroundColor: const Color(0xFF133E4D),
                    foregroundColor: Colors.white,
                    padding: const EdgeInsets.symmetric(vertical: 14.0),
                    shape: RoundedRectangleBorder(
                      borderRadius: BorderRadius.circular(24.0),
                    ),
                    elevation: 0,
                  ),
                ),
              ),
            ],
          ),
        ],
      ),
    );
  }
}

