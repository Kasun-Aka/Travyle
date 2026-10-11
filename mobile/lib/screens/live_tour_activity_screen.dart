import 'dart:convert';
import 'package:flutter/material.dart';
import 'package:flutter_map/flutter_map.dart';
import 'package:http/http.dart' as http;
import 'package:latlong2/latlong.dart';
import '../services/api_config.dart';

class LiveTourActivityScreen extends StatefulWidget {
  final bool isGuide;
  const LiveTourActivityScreen({super.key, this.isGuide = false});

  @override
  State<LiveTourActivityScreen> createState() => _LiveTourActivityScreenState();
}

class _LiveTourActivityScreenState extends State<LiveTourActivityScreen> {
  String? _activeAlertMessage;
  bool _isLoadingMonitor = false;
  bool _isLoadingData = true;
  String _tourName = 'Loading...';
  String _guideName = '';
  bool _hasDisruption = false;
  String? _selectedScheduleId;
  List<Map<String, dynamic>> _checklist = [];
  List<Map<String, dynamic>> _activeTours = [];
  List<Map<String, dynamic>> _allAlerts = [];

  String get _baseUrl => ApiConfig.baseUrl;

  @override
  void initState() {
    super.initState();
    _fetchLiveData();
  }

  Future<void> _fetchLiveData() async {
    setState(() => _isLoadingData = true);
    try {
      // 1. Fetch active tours from live-operations
      final opsResponse = await http.get(
        Uri.parse('$_baseUrl/api/admin/live-operations'),
      );

      if (opsResponse.statusCode == 200) {
        final opsData = jsonDecode(opsResponse.body);
        final tours = (opsData['activeTours'] as List?) ?? [];
        final alerts = (opsData['alerts'] as List?) ?? [];

        setState(() {
          _allAlerts = alerts.map<Map<String, dynamic>>((a) => a as Map<String, dynamic>).toList();
          _activeTours = tours.map<Map<String, dynamic>>((t) => {
            'id': t['id'] as String,
            'name': t['name'] as String? ?? 'Unknown Tour',
            'guide': t['guide'] as String? ?? '',
            'lat': t['currentLocation'] != null
                ? double.tryParse(t['currentLocation']['lat'].toString()) ?? 0.0
                : 0.0,
            'lon': t['currentLocation'] != null
                ? double.tryParse(t['currentLocation']['lon'].toString()) ?? 0.0
                : 0.0,
            'status': t['status'] as String? ?? 'On Time',
            'progress': t['progress'] as String? ?? '0/0',
          }).toList();
        });

        // 2. Auto-select first tour and fetch its route log
        if (tours.isNotEmpty) {
          final firstTourId = tours[0]['id'] as String;
          await _fetchRouteLog(firstTourId);
        }
      }
    } catch (e) {
      debugPrint('Error fetching live data: $e');
    } finally {
      setState(() => _isLoadingData = false);
    }
  }

  Future<void> _fetchRouteLog(String scheduleId) async {
    setState(() => _selectedScheduleId = scheduleId);
    try {
      final response = await http.get(
        Uri.parse('$_baseUrl/api/admin/route-log/$scheduleId'),
      );

      if (response.statusCode == 200) {
        final data = jsonDecode(response.body);
        final stops = (data['stops'] as List?) ?? [];

        setState(() {
          _tourName = data['tourName'] as String? ?? 'Unknown Tour';
          _guideName = data['guideName'] as String? ?? '';
          _hasDisruption = data['hasDisruption'] as bool? ?? false;
          
          if (_hasDisruption) {
            try {
              final alert = _allAlerts.firstWhere((a) => a['tourId'] == scheduleId);
              _activeAlertMessage = alert['description'] as String?;
            } catch (_) {
              _activeAlertMessage = 'Disruption active on route.';
            }
          } else if (!_isLoadingMonitor) {
            _activeAlertMessage = null;
          }
          
          _checklist = stops.map<Map<String, dynamic>>((s) {
            final status = s['status'] as String? ?? 'Scheduled';
            CheckStatus checkStatus;
            switch (status) {
              case 'Completed':
                checkStatus = CheckStatus.completed;
                break;
              case 'InProgress':
                checkStatus = CheckStatus.current;
                break;
              default:
                checkStatus = CheckStatus.upcoming;
            }
            return {
              'id': s['id'] as String? ?? '',
              'title': s['name'] as String? ?? '',
              'estimatedTime': 'Estimated: ${s['time'] ?? ''} • ${s['location'] ?? ''}',
              'status': checkStatus,
            };
          }).toList();
        });
      }
    } catch (e) {
      debugPrint('Error fetching route log: $e');
    }
  }

  Future<void> _updateStopStatus(String activityId, String newStatus) async {
    if (activityId.isEmpty) return;
    try {
      final response = await http.put(
        Uri.parse('$_baseUrl/api/operations/activities/$activityId/status'),
        headers: {'Content-Type': 'application/json'},
        body: jsonEncode({'status': newStatus}),
      );
      if (response.statusCode != 200) {
        debugPrint('Failed to update status: ${response.body}');
      }
    } catch (e) {
      debugPrint('Error updating stop status: $e');
    }
  }

  Future<void> _monitorOperations() async {
    setState(() => _isLoadingMonitor = true);
    try {
      // Use real schedule ID and coordinates from selected tour
      final tourId = _selectedScheduleId ?? '00000000-0000-0000-0001-000000000001';
      final tour = _activeTours.firstWhere(
        (t) => t['id'] == tourId,
        orElse: () => {'lat': 6.8711, 'lon': 81.0458},
      );

      final response = await http.post(
        Uri.parse('$_baseUrl/api/operations/monitor'),
        headers: {'Content-Type': 'application/json'},
        body: jsonEncode({
          'bookingScheduleId': tourId,
          'lat': tour['lat'] ?? 6.8711,
          'lon': tour['lon'] ?? 81.0458,
        }),
      );
      if (response.statusCode == 200) {
        final data = jsonDecode(response.body);
        setState(() {
          if (data['status'] == 'DISRUPTION_DETECTED') {
            _activeAlertMessage = data['message'] ?? 'Disruption detected on route!';
          } else {
            _activeAlertMessage = data['message'] ?? 'Route is clear. No disruptions.';
          }
        });
      } else {
        setState(() => _activeAlertMessage = 'AI Monitor returned status ${response.statusCode}. Check agent server.');
      }
    } catch (e) {
      setState(() => _activeAlertMessage = 'Error connecting to server. Make sure both backend and agent are running.');
    } finally {
      setState(() => _isLoadingMonitor = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    // Only show the selected tour on the map
    final selectedTour = _activeTours.where((t) => t['id'] == _selectedScheduleId && (t['lat'] as double) != 0.0).toList();
    final mapLat = selectedTour.isNotEmpty ? selectedTour[0]['lat'] as double : 6.8711;
    final mapLon = selectedTour.isNotEmpty ? selectedTour[0]['lon'] as double : 81.0458;
    final mapZoom = 13.0;

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
          children: [
            const Text(
              'Live Tour Activity',
              style: TextStyle(
                color: Color(0xFF133E4D),
                fontSize: 20.0,
                fontWeight: FontWeight.bold,
              ),
            ),
            Text(
              _tourName,
              style: const TextStyle(
                color: Colors.grey,
                fontSize: 13.0,
              ),
            ),
          ],
        ),
      ),
      body: _isLoadingData
          ? const Center(child: CircularProgressIndicator())
          : SingleChildScrollView(
              padding: const EdgeInsets.symmetric(horizontal: 20.0, vertical: 16.0),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  // Tour selector chips (if multiple active tours)
                  if (_activeTours.length > 1) ...[
                    SizedBox(
                      height: 40,
                      child: ListView.separated(
                        scrollDirection: Axis.horizontal,
                        itemCount: _activeTours.length,
                        separatorBuilder: (_, __) => const SizedBox(width: 8),
                        itemBuilder: (context, index) {
                          final tour = _activeTours[index];
                          final isSelected = tour['id'] == _selectedScheduleId;
                          final shortName = (tour['name'] as String).split(' ').take(3).join(' ');
                          return GestureDetector(
                            onTap: () => _fetchRouteLog(tour['id'] as String),
                            child: Container(
                              padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 8),
                              decoration: BoxDecoration(
                                color: isSelected ? const Color(0xFF133E4D) : Colors.white,
                                borderRadius: BorderRadius.circular(20),
                                border: Border.all(
                                  color: isSelected ? const Color(0xFF133E4D) : Colors.grey.shade300,
                                ),
                              ),
                              child: Text(
                                shortName,
                                style: TextStyle(
                                  fontSize: 12,
                                  fontWeight: FontWeight.w600,
                                  color: isSelected ? Colors.white : const Color(0xFF133E4D),
                                ),
                              ),
                            ),
                          );
                        },
                      ),
                    ),
                    const SizedBox(height: 16),
                  ],

                  // Live Map with dynamic markers
                  _buildLiveMap(mapLat, mapLon, mapZoom, selectedTour),
                  const SizedBox(height: 24.0),

                  // Guide info
                  if (_guideName.isNotEmpty)
                    Padding(
                      padding: const EdgeInsets.only(bottom: 16),
                      child: Row(
                        children: [
                          const Icon(Icons.person, size: 18, color: Color(0xFF133E4D)),
                          const SizedBox(width: 8),
                          Text(
                            'Guide: $_guideName',
                            style: const TextStyle(
                              fontSize: 14,
                              fontWeight: FontWeight.w600,
                              color: Color(0xFF133E4D),
                            ),
                          ),
                          if (_hasDisruption) ...[
                            const Spacer(),
                            Container(
                              padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
                              decoration: BoxDecoration(
                                color: Colors.orange.shade50,
                                borderRadius: BorderRadius.circular(12),
                                border: Border.all(color: Colors.orange.shade200),
                              ),
                              child: Row(
                                mainAxisSize: MainAxisSize.min,
                                children: [
                                  Icon(Icons.warning_amber, size: 14, color: Colors.orange.shade700),
                                  const SizedBox(width: 4),
                                  Text(
                                    'Disruption',
                                    style: TextStyle(
                                      fontSize: 11,
                                      fontWeight: FontWeight.w700,
                                      color: Colors.orange.shade700,
                                    ),
                                  ),
                                ],
                              ),
                            ),
                          ],
                        ],
                      ),
                    ),

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

                  if (_checklist.isEmpty)
                    const Padding(
                      padding: EdgeInsets.symmetric(vertical: 24),
                      child: Center(
                        child: Text(
                          'No active tour stops found',
                          style: TextStyle(color: Colors.grey, fontSize: 14),
                        ),
                      ),
                    )
                  else
                    ..._checklist.asMap().entries.map((entry) {
                      int idx = entry.key;
                      var item = entry.value;
                      return _buildChecklistItem(
                        title: item['title'] as String,
                        estimatedTime: item['estimatedTime'] as String,
                        status: item['status'] as CheckStatus,
                        onTap: widget.isGuide ? () {
                          CheckStatus newStatus;
                          String backendStatus;
                          if (item['status'] == CheckStatus.upcoming) {
                            newStatus = CheckStatus.current;
                            backendStatus = 'InProgress';
                          } else if (item['status'] == CheckStatus.current) {
                            newStatus = CheckStatus.completed;
                            backendStatus = 'Completed';
                          } else {
                            newStatus = CheckStatus.upcoming;
                            backendStatus = 'Scheduled';
                          }
                          setState(() {
                            _checklist[idx]['status'] = newStatus;
                          });
                          _updateStopStatus(item['id'] as String, backendStatus);
                        } : null,
                      );
                    }),

                  const SizedBox(height: 32.0),
                  const SizedBox(height: 16.0),

                  // Alert display
                  if (_activeAlertMessage != null)
                    Container(
                      margin: const EdgeInsets.only(bottom: 16.0),
                      padding: const EdgeInsets.all(16.0),
                      decoration: BoxDecoration(
                        color: _activeAlertMessage!.contains('clear')
                            ? Colors.green.shade50
                            : Colors.red.shade50,
                        borderRadius: BorderRadius.circular(12.0),
                        border: Border.all(
                          color: _activeAlertMessage!.contains('clear')
                              ? Colors.green.shade200
                              : Colors.red.shade200,
                        ),
                      ),
                      child: Row(
                        children: [
                          Icon(
                            _activeAlertMessage!.contains('clear')
                                ? Icons.check_circle
                                : Icons.warning,
                            color: _activeAlertMessage!.contains('clear')
                                ? Colors.green
                                : Colors.red,
                          ),
                          const SizedBox(width: 12.0),
                          Expanded(
                            child: Text(
                              _activeAlertMessage!,
                              style: TextStyle(
                                color: _activeAlertMessage!.contains('clear')
                                    ? Colors.green.shade800
                                    : Colors.red,
                                fontWeight: FontWeight.bold,
                              ),
                            ),
                          ),
                        ],
                      ),
                    ),

                  // AI Monitor button
                  SizedBox(
                    width: double.infinity,
                    child: ElevatedButton.icon(
                      onPressed: _isLoadingMonitor ? null : _monitorOperations,
                      icon: _isLoadingMonitor
                          ? const SizedBox(width: 16, height: 16, child: CircularProgressIndicator(strokeWidth: 2))
                          : const Icon(Icons.satellite_alt, color: Colors.white),
                      label: Text(
                        _isLoadingMonitor ? 'ANALYZING ROUTE...' : 'AI MONITOR ROUTE & WEATHER',
                        style: const TextStyle(
                          fontSize: 14.0,
                          fontWeight: FontWeight.w700,
                          letterSpacing: 0.5,
                        ),
                      ),
                      style: ElevatedButton.styleFrom(
                        padding: const EdgeInsets.symmetric(vertical: 16.0),
                        backgroundColor: const Color(0xFF133E4D),
                        foregroundColor: Colors.white,
                        shape: RoundedRectangleBorder(
                          borderRadius: BorderRadius.circular(24.0),
                        ),
                      ),
                    ),
                  ),
                  const SizedBox(height: 16.0),
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

  Widget _buildLiveMap(double lat, double lon, double zoom, List<Map<String, dynamic>> selectedTour) {
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
              options: MapOptions(
                initialCenter: LatLng(lat, lon),
                initialZoom: zoom,
              ),
              children: [
                TileLayer(
                  urlTemplate: 'https://tile.openstreetmap.org/{z}/{x}/{y}.png',
                  userAgentPackageName: 'com.travyle.app',
                ),
                // Weather/status zone for the selected tour only
                CircleLayer(
                  circles: selectedTour.map((t) {
                    final isDelayed = t['status'] == 'Delayed';
                    return CircleMarker(
                      point: LatLng(t['lat'] as double, t['lon'] as double),
                      color: isDelayed
                          ? Colors.orange.withAlpha(51)
                          : Colors.green.withAlpha(51),
                      borderStrokeWidth: 0,
                      useRadiusInMeter: true,
                      radius: 800,
                    );
                  }).toList(),
                ),
                // Selected tour marker only
                MarkerLayer(
                  markers: selectedTour.map((t) {
                    final isDelayed = t['status'] == 'Delayed';
                    final shortName = (t['name'] as String).split(' ').take(2).join(' ');
                    return Marker(
                      point: LatLng(t['lat'] as double, t['lon'] as double),
                      width: 130,
                      height: 30,
                      child: Container(
                        padding: const EdgeInsets.symmetric(horizontal: 10.0, vertical: 5.0),
                        decoration: BoxDecoration(
                          color: Colors.blue.shade600,
                          borderRadius: BorderRadius.circular(20.0),
                          boxShadow: const [
                            BoxShadow(color: Colors.black26, blurRadius: 4, offset: Offset(0, 2)),
                          ],
                        ),
                        child: Row(
                          mainAxisSize: MainAxisSize.min,
                          mainAxisAlignment: MainAxisAlignment.center,
                          children: [
                            if (isDelayed)
                              Container(
                                width: 8,
                                height: 8,
                                margin: const EdgeInsets.only(right: 6),
                                decoration: const BoxDecoration(
                                  color: Colors.orange,
                                  shape: BoxShape.circle,
                                ),
                              ),
                            Flexible(
                              child: Text(
                                shortName,
                                overflow: TextOverflow.ellipsis,
                                style: const TextStyle(
                                  color: Colors.white,
                                  fontSize: 10.0,
                                  fontWeight: FontWeight.bold,
                                ),
                              ),
                            ),
                          ],
                        ),
                      ),
                    );
                  }).toList(),
                ),
              ],
            ),
            // Live GPS badge
            Positioned(
              top: 16,
              left: 16,
              child: Container(
                padding: const EdgeInsets.symmetric(horizontal: 12.0, vertical: 6.0),
                decoration: BoxDecoration(
                  color: Colors.white.withAlpha(230),
                  borderRadius: BorderRadius.circular(20.0),
                  boxShadow: const [
                    BoxShadow(color: Colors.black12, blurRadius: 4, offset: Offset(0, 2)),
                  ],
                ),
                child: Row(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    const CircleAvatar(radius: 4, backgroundColor: Color(0xFF1FA88A)),
                    const SizedBox(width: 8),
                    Text(
                      _tourName == 'Loading...' ? 'LIVE TOUR' : _tourName.toUpperCase(),
                      style: const TextStyle(
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
