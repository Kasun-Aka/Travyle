import 'package:flutter/material.dart';
import 'package:mobile/screens/live_tour_activity_screen.dart';
import 'package:mobile/screens/qr_checkin_screen.dart';

class GuideDashboardScreen extends StatefulWidget {
  const GuideDashboardScreen({super.key});

  @override
  State<GuideDashboardScreen> createState() => _GuideDashboardScreenState();
}

class _GuideDashboardScreenState extends State<GuideDashboardScreen> {
  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: const Color(0xFFF8F9FA), // Light background
      body: SafeArea(
        child: SingleChildScrollView(
          padding: const EdgeInsets.symmetric(horizontal: 20.0, vertical: 24.0),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              _buildHeader(context),
              const SizedBox(height: 24.0),
              _buildStatsRow(),
              const SizedBox(height: 32.0),
              const Text(
                'Today\'s Assigned Tours',
                style: TextStyle(
                  fontSize: 18.0,
                  fontWeight: FontWeight.w700,
                  color: Color(0xFF133E4D), // Dark blue/teal
                ),
              ),
              const SizedBox(height: 16.0),
              _buildTourCard(
                time: '09:00 AM - 12:00 PM',
                travelers: '6 Travelers',
                title: 'Sacred Ubud Forest Walk',
                location: 'Ubud Monkey Forest Main Entrance',
                actionText: 'CHECK IN / RESUME TOUR',
                actionColor: const Color(0xFFDD8866), // Accent orange
                onActionPressed: () {
                  Navigator.push(
                    context,
                    MaterialPageRoute(
                      builder: (context) => const QRCheckinScreen(),
                    ),
                  );
                },
              ),
              const SizedBox(height: 16.0),
              _buildTourCard(
                time: '04:30 PM - 07:30 PM',
                travelers: '8 Travelers',
                title: 'Sunset Tanah Lot Escape',
                location: 'Tanah Lot Temple Lobby',
                actionText: 'START TOUR',
                actionColor: const Color(0xFF133E4D),
                onActionPressed: () {
                  Navigator.push(
                    context,
                    MaterialPageRoute(
                      builder: (context) => const LiveTourActivityScreen(),
                    ),
                  );
                },
              ),
            ],
          ),
        ),
      ),
    );
  }

  Widget _buildHeader(BuildContext context) {
    return Row(
      children: [
        const CircleAvatar(
          radius: 24.0,
          backgroundImage: NetworkImage(
              'https://ui-avatars.com/api/?name=Ketut+Alit&background=DD8866&color=fff'), // Placeholder
        ),
        const SizedBox(width: 12.0),
        Expanded(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: const [
              Text(
                'LOCAL GUIDE',
                style: TextStyle(
                  fontSize: 12.0,
                  fontWeight: FontWeight.bold,
                  color: Colors.grey,
                  letterSpacing: 0.5,
                ),
              ),
              Text(
                'Ketut Alit',
                style: TextStyle(
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
            child: const Icon(
              Icons.qr_code_scanner,
              color: Color(0xFF133E4D),
            ),
          ),
        ),
      ],
    );
  }

  Widget _buildStatsRow() {
    return Row(
      mainAxisAlignment: MainAxisAlignment.spaceBetween,
      children: [
        _buildStatCard('2', 'TOURS TODAY'),
        _buildStatCard('14', 'TRAVELERS'),
        _buildStatCard('4.9', 'MY RATING'),
      ],
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
