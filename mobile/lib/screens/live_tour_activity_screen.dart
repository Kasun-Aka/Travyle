import 'package:flutter/material.dart';
import 'package:flutter_map/flutter_map.dart';
import 'package:latlong2/latlong.dart';

class LiveTourActivityScreen extends StatefulWidget {
  final bool isGuide;
  const LiveTourActivityScreen({super.key, this.isGuide = false});

  @override
  State<LiveTourActivityScreen> createState() => _LiveTourActivityScreenState();
}

class _LiveTourActivityScreenState extends State<LiveTourActivityScreen> {
  final List<Map<String, dynamic>> _checklist = [
    {'title': 'Tanah Lot Temple Sunrise', 'estimatedTime': 'Estimated: 06:15 AM', 'status': CheckStatus.completed},
    {'title': 'Kopi Luwak Estate', 'estimatedTime': 'Estimated: 08:30 AM • CURRENT STOP', 'status': CheckStatus.current},
    {'title': 'Bratan Volcanic Caldera', 'estimatedTime': 'Estimated: 11:00 AM', 'status': CheckStatus.upcoming},
    {'title': 'Ubud Art Market Lounge', 'estimatedTime': 'Estimated: 02:00 PM', 'status': CheckStatus.upcoming},
  ];


  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: const Color(0xFFF8F9FA),
      appBar: AppBar(
        backgroundColor: Colors.transparent,
        elevation: 0,
        leading: Padding(
          padding: const EdgeInsets.only(left: 20.0),
          child: InkWell(
            onTap: () => Navigator.pop(context),
            borderRadius: BorderRadius.circular(24.0),
            child: Container(
              decoration: BoxDecoration(
                shape: BoxShape.circle,
                border: Border.all(color: Colors.grey.shade300),
                color: Colors.white,
              ),
              child: const Icon(Icons.arrow_back_ios_new,
                  color: Color(0xFF133E4D), size: 16),
            ),
          ),
        ),
        title: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: const [
            Text(
              'Live Tour Activity',
              style: TextStyle(
                color: Color(0xFF133E4D),
                fontSize: 20.0,
                fontWeight: FontWeight.bold,
              ),
            ),
            Text(
              'Tanah Lot & Ubud Day Tour',
              style: TextStyle(
                color: Colors.grey,
                fontSize: 13.0,
              ),
            ),
          ],
        ),
      ),
      body: SingleChildScrollView(
        padding: const EdgeInsets.symmetric(horizontal: 20.0, vertical: 16.0),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            _buildMapPlaceholder(),
            const SizedBox(height: 24.0),
            const Text(
              'TOUR STOPS & CHECKLIST',
              style: TextStyle(
                fontSize: 14.0,
                fontWeight: FontWeight.w800,
                color: Color(0xFF133E4D),
                letterSpacing: 0.5,
              ),
            ),
            const SizedBox(height: 16.0),
            ..._checklist.asMap().entries.map((entry) {
              int idx = entry.key;
              var item = entry.value;
              return _buildChecklistItem(
                title: item['title'] as String,
                estimatedTime: item['estimatedTime'] as String,
                status: item['status'] as CheckStatus,
                onTap: widget.isGuide ? () {
                  setState(() {
                    if (item['status'] == CheckStatus.upcoming) {
                      _checklist[idx]['status'] = CheckStatus.current;
                    } else if (item['status'] == CheckStatus.current) {
                      _checklist[idx]['status'] = CheckStatus.completed;
                    } else {
                      _checklist[idx]['status'] = CheckStatus.upcoming;
                    }
                  });
                } : null,
              );
            }).toList(),
            const SizedBox(height: 32.0),
            SizedBox(
              width: double.infinity,
              child: OutlinedButton.icon(
                onPressed: () {},
                icon: const Icon(Icons.warning_amber_rounded,
                    color: Color(0xFFDD8866)),
                label: const Text(
                  'REPORT INCIDENT / DISRUPTION',
                  style: TextStyle(
                    fontSize: 14.0,
                    fontWeight: FontWeight.w700,
                    color: Color(0xFFDD8866),
                    letterSpacing: 0.5,
                  ),
                ),
                style: OutlinedButton.styleFrom(
                  padding: const EdgeInsets.symmetric(vertical: 16.0),
                  side: const BorderSide(color: Color(0xFFDD8866)),
                  shape: RoundedRectangleBorder(
                    borderRadius: BorderRadius.circular(24.0),
                  ),
                ),
              ),
            ),
            const SizedBox(height: 24.0),
          ],
        ),
      ),
    );
  }

  Widget _buildMapPlaceholder() {
    return ClipRRect(
      borderRadius: BorderRadius.circular(16.0),
      child: Container(
        height: 200,
        width: double.infinity,
        decoration: BoxDecoration(
          color: const Color(0xFF133E4D),
          borderRadius: BorderRadius.circular(16.0),
        ),
        child: Stack(
          children: [
            FlutterMap(
              options: const MapOptions(
                initialCenter: LatLng(6.8711, 81.0458),
                initialZoom: 13.0,
              ),
              children: [
                TileLayer(
                  urlTemplate: 'https://tile.openstreetmap.org/{z}/{x}/{y}.png',
                  userAgentPackageName: 'com.travyle.app',
                ),
                CircleLayer(
                  circles: [
                    CircleMarker(
                      point: const LatLng(6.8811, 81.0358),
                      color: Colors.green.withOpacity(0.2),
                      borderStrokeWidth: 0,
                      useRadiusInMeter: true,
                      radius: 800,
                    ),
                    CircleMarker(
                      point: const LatLng(6.8711, 81.0658),
                      color: Colors.orange.withOpacity(0.2),
                      borderStrokeWidth: 0,
                      useRadiusInMeter: true,
                      radius: 500,
                    ),
                  ],
                ),
                MarkerLayer(
                  markers: [
                    Marker(
                      point: const LatLng(6.8711, 81.0458),
                      width: 100,
                      height: 30,
                      child: Container(
                        padding: const EdgeInsets.symmetric(horizontal: 12.0, vertical: 6.0),
                        decoration: BoxDecoration(
                          color: Colors.blue.shade600,
                          borderRadius: BorderRadius.circular(20.0),
                          boxShadow: const [
                            BoxShadow(color: Colors.black26, blurRadius: 4, offset: Offset(0, 2)),
                          ],
                        ),
                        child: const Center(
                          child: Text(
                            'TOUR-5510',
                            style: TextStyle(
                              color: Colors.white,
                              fontSize: 10.0,
                              fontWeight: FontWeight.bold,
                            ),
                          ),
                        ),
                      ),
                    ),
                  ],
                ),
              ],
            ),
            Positioned(
              top: 16,
              left: 16,
              child: Container(
                padding: const EdgeInsets.symmetric(horizontal: 12.0, vertical: 6.0),
                decoration: BoxDecoration(
                  color: Colors.white.withOpacity(0.9),
                  borderRadius: BorderRadius.circular(20.0),
                  boxShadow: const [
                    BoxShadow(color: Colors.black12, blurRadius: 4, offset: Offset(0, 2)),
                  ],
                ),
                child: Row(
                  mainAxisSize: MainAxisSize.min,
                  children: const [
                    CircleAvatar(radius: 4, backgroundColor: Color(0xFF1FA88A)),
                    SizedBox(width: 8),
                    Text(
                      'LIVE GPS ACTIVE',
                      style: TextStyle(
                        fontSize: 10.0,
                        fontWeight: FontWeight.w800,
                        color: Color(0xFF133E4D),
                        letterSpacing: 0.5,
                      ),
                    ),
                  ],
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildChecklistItem({
    required String title,
    required String estimatedTime,
    required CheckStatus status,
    VoidCallback? onTap,
  }) {
    Color getBgColor() {
      switch (status) {
        case CheckStatus.current:
          return const Color(0xFFF1F4F5);
        default:
          return Colors.white;
      }
    }

    Widget getIcon() {
      switch (status) {
        case CheckStatus.completed:
          return Container(
            width: 24,
            height: 24,
            decoration: BoxDecoration(
              color: const Color(0xFF133E4D),
              borderRadius: BorderRadius.circular(6.0),
            ),
            child: const Icon(Icons.check, color: Colors.white, size: 16),
          );
        case CheckStatus.current:
          return Container(
            width: 24,
            height: 24,
            decoration: BoxDecoration(
              border: Border.all(color: const Color(0xFFDD8866)),
              borderRadius: BorderRadius.circular(6.0),
            ),
            child: Center(
              child: Container(
                width: 10,
                height: 10,
                decoration: const BoxDecoration(
                  color: Color(0xFFDD8866),
                  shape: BoxShape.circle,
                ),
              ),
            ),
          );
        case CheckStatus.upcoming:
          return Container(
            width: 24,
            height: 24,
            decoration: BoxDecoration(
              border: Border.all(color: Colors.grey.shade400),
              borderRadius: BorderRadius.circular(6.0),
            ),
          );
      }
    }

    return GestureDetector(
      onTap: onTap,
      child: Container(
        margin: const EdgeInsets.only(bottom: 12.0),
        padding: const EdgeInsets.all(16.0),
        decoration: BoxDecoration(
          color: getBgColor(),
          borderRadius: BorderRadius.circular(16.0),
          border: Border.all(color: Colors.grey.shade200),
        ),
        child: Row(
          children: [
            getIcon(),
            const SizedBox(width: 16.0),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    title,
                    style: TextStyle(
                      fontSize: 16.0,
                      fontWeight: FontWeight.w700,
                      color: status == CheckStatus.completed
                          ? Colors.grey
                          : const Color(0xFF133E4D),
                      decoration: status == CheckStatus.completed
                          ? TextDecoration.lineThrough
                          : null,
                    ),
                  ),
                  const SizedBox(height: 4.0),
                  Text(
                    estimatedTime,
                    style: const TextStyle(
                      fontSize: 13.0,
                      color: Colors.grey,
                    ),
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}

enum CheckStatus { completed, current, upcoming }
