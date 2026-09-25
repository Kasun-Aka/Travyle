import 'dart:async';
import 'package:flutter/material.dart';
import 'package:dio/dio.dart';
import '../theme/app_theme.dart';
import 'destination_detail_screen.dart';

class DestinationsListScreen extends StatefulWidget {
  const DestinationsListScreen({super.key});

  @override
  State<DestinationsListScreen> createState() => _DestinationsListScreenState();
}

class _DestinationsListScreenState extends State<DestinationsListScreen> {
  bool _isLoading = true;
  List<dynamic> _destinations = [];
  String _searchQuery = "";
  Timer? _debounce;

  @override
  void dispose() {
    _debounce?.cancel();
    super.dispose();
  }

  @override
  void initState() {
    super.initState();
    _fetchDestinations();
  }

  Future<void> _fetchDestinations() async {
    setState(() => _isLoading = true);
    try {
      final dio = Dio();
      final url = _searchQuery.isNotEmpty
          ? 'http://10.0.2.2:5085/api/Destinations?search=$_searchQuery'
          : 'http://10.0.2.2:5085/api/Destinations';
      final response = await dio.get(url);
      if (response.statusCode == 200 && response.data != null) {
        if (mounted) {
          setState(() {
            _destinations = response.data['items'] ?? [];
            _isLoading = false;
          });
        }
      }
    } catch (e) {
      debugPrint('Error fetching destinations: $e');
      if (mounted) {
        setState(() => _isLoading = false);
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppTheme.backgroundLight,
      appBar: AppBar(
        title: const Text(
          'Explore Destinations',
          style: TextStyle(
            color: AppTheme.primaryDark,
            fontWeight: FontWeight.bold,
          ),
        ),
        backgroundColor: Colors.white,
        elevation: 0,
        iconTheme: const IconThemeData(color: AppTheme.primaryDark),
      ),
      body: Column(
        children: [
          // Search Bar
          Container(
            color: Colors.white,
            padding: const EdgeInsets.symmetric(
              horizontal: 24.0,
              vertical: 16.0,
            ),
            child: TextField(
              onChanged: (value) {
                _searchQuery = value;
                if (_debounce?.isActive ?? false) _debounce!.cancel();
                _debounce = Timer(const Duration(milliseconds: 500), () {
                  _fetchDestinations();
                });
              },
              onSubmitted: (value) {
                if (_debounce?.isActive ?? false) _debounce!.cancel();
                _fetchDestinations();
              },
              decoration: InputDecoration(
                hintText: 'Search places, regions...',
                prefixIcon: const Icon(Icons.search, color: AppTheme.textGrey),
                suffixIcon: IconButton(
                  icon: const Icon(Icons.send, color: AppTheme.accentCopper),
                  onPressed: _fetchDestinations,
                ),
                filled: true,
                fillColor: AppTheme.backgroundLight,
                border: OutlineInputBorder(
                  borderRadius: BorderRadius.circular(16),
                  borderSide: BorderSide.none,
                ),
                contentPadding: const EdgeInsets.symmetric(vertical: 16),
              ),
            ),
          ),

          // Destinations List
          Expanded(
            child: _isLoading
                ? const Center(
                    child: CircularProgressIndicator(
                      color: AppTheme.primaryDark,
                    ),
                  )
                : _destinations.isEmpty
                ? const Center(child: Text('No destinations found.'))
                : ListView.builder(
                    padding: const EdgeInsets.all(24.0),
                    itemCount: _destinations.length,
                    itemBuilder: (context, index) {
                      final dest = _destinations[index];
                      return _buildListCard(context, dest);
                    },
                  ),
          ),
        ],
      ),
    );
  }

  Widget _buildListCard(BuildContext context, Map<String, dynamic> dest) {
    final title = dest['name'] ?? 'Unknown';
    final region = dest['region'] ?? 'Unknown Region';
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
        margin: const EdgeInsets.only(bottom: 24),
        decoration: BoxDecoration(
          color: Colors.white,
          borderRadius: BorderRadius.circular(20),
          boxShadow: [
            BoxShadow(
              color: Colors.black.withValues(alpha: 0.04),
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
            Padding(
              padding: const EdgeInsets.all(16.0),
              child: Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          title,
                          style: const TextStyle(
                            fontWeight: FontWeight.bold,
                            fontSize: 18,
                            color: AppTheme.primaryDark,
                          ),
                          maxLines: 1,
                          overflow: TextOverflow.ellipsis,
                        ),
                        const SizedBox(height: 4),
                        Text(
                          region,
                          style: const TextStyle(
                            color: AppTheme.textGrey,
                            fontSize: 14,
                          ),
                          maxLines: 1,
                          overflow: TextOverflow.ellipsis,
                        ),
                      ],
                    ),
                  ),
                  Row(
                    children: [
                      const Icon(Icons.star, color: Colors.amber, size: 20),
                      const SizedBox(width: 4),
                      Text(
                        rating,
                        style: const TextStyle(
                          fontWeight: FontWeight.bold,
                          fontSize: 16,
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
}
