import 'package:flutter/material.dart';
import '../../models/customer_review.dart';
import '../../services/support_api_service.dart';
import '../../theme/app_theme.dart';

class TourReviewScreen extends StatefulWidget {
  final String tourId;
  final String tourTitle;

  const TourReviewScreen({
    super.key,
    this.tourId = '33333333-3333-3333-3333-333333333333',
    this.tourTitle = 'Alpine Excursion & Scenic Rail Tour',
  });

  @override
  State<TourReviewScreen> createState() => _TourReviewScreenState();
}

class _TourReviewScreenState extends State<TourReviewScreen> {
  final SupportApiService _apiService = SupportApiService();
  int _selectedRating = 5;
  final _commentController = TextEditingController();
  bool _isSubmitting = false;

  List<CustomerReviewModel> _reviews = [];
  bool _isLoadingReviews = true;

  @override
  void initState() {
    super.initState();
    _loadReviews();
  }

  @override
  void dispose() {
    _commentController.dispose();
    super.dispose();
  }

  Future<void> _loadReviews() async {
    setState(() => _isLoadingReviews = true);
    final results = await _apiService.getReviewsByTour(widget.tourId);
    if (!mounted) return;
    setState(() {
      _reviews = results;
      _isLoadingReviews = false;
    });
  }

  Future<void> _submitReview() async {
    if (_commentController.text.trim().isEmpty) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Please enter your review comments')),
      );
      return;
    }

    setState(() => _isSubmitting = true);
    final created = await _apiService.createReview(
      tourId: widget.tourId,
      rating: _selectedRating,
      comment: _commentController.text.trim(),
    );

    if (!mounted) return;
    setState(() => _isSubmitting = false);

    if (created != null) {
      _commentController.clear();
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('Review submitted successfully! Thank you for your feedback.'),
          backgroundColor: Colors.green,
        ),
      );
      _loadReviews();
    } else {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('Failed to submit review. Check backend connection.'),
          backgroundColor: Colors.redAccent,
        ),
      );
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Tour Ratings & Feedback', style: TextStyle(color: Colors.white, fontWeight: FontWeight.bold)),
        backgroundColor: AppTheme.primaryDark,
        iconTheme: const IconThemeData(color: Colors.white),
      ),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(20),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // Header Info
            Container(
              width: double.infinity,
              padding: const EdgeInsets.all(16),
              decoration: BoxDecoration(
                color: const Color(0xFFF8F9FB),
                borderRadius: BorderRadius.circular(12),
                border: Border.all(color: const Color(0xFFE2E8F0)),
              ),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  const Text('TOUR EXPERIENCE', style: TextStyle(color: AppTheme.coral, fontSize: 11, fontWeight: FontWeight.bold)),
                  const SizedBox(height: 4),
                  Text(widget.tourTitle, style: const TextStyle(fontSize: 16, fontWeight: FontWeight.bold, color: AppTheme.primaryDark)),
                ],
              ),
            ),
            const SizedBox(height: 20),

            // Rating Stars
            const Text('Rate Your Experience', style: TextStyle(fontSize: 15, fontWeight: FontWeight.bold, color: AppTheme.textDark)),
            const SizedBox(height: 8),
            Row(
              mainAxisAlignment: MainAxisAlignment.center,
              children: List.generate(5, (index) {
                final starNum = index + 1;
                return IconButton(
                  iconSize: 36,
                  icon: Icon(
                    starNum <= _selectedRating ? Icons.star : Icons.star_border,
                    color: const Color(0xFFF59E0B),
                  ),
                  onPressed: () => setState(() => _selectedRating = starNum),
                );
              }),
            ),
            Center(
              child: Text(
                '$_selectedRating out of 5 Stars',
                style: const TextStyle(fontWeight: FontWeight.w600, color: Color(0xFFD97706)),
              ),
            ),
            const SizedBox(height: 16),

            // Comment Box
            const Text('Feedback & Comments', style: TextStyle(fontWeight: FontWeight.w600, color: AppTheme.textDark)),
            const SizedBox(height: 8),
            TextField(
              controller: _commentController,
              maxLines: 3,
              decoration: const InputDecoration(
                hintText: 'Share your highlights, feedback on guides, or areas for improvement...',
              ),
            ),
            const SizedBox(height: 18),

            // Submit Button
            SizedBox(
              width: double.infinity,
              height: 48,
              child: ElevatedButton(
                onPressed: _isSubmitting ? null : _submitReview,
                style: ElevatedButton.styleFrom(
                  backgroundColor: AppTheme.primaryDark,
                  shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(30)),
                ),
                child: _isSubmitting
                    ? const CircularProgressIndicator(color: Colors.white)
                    : const Text('Post Verified Review', style: TextStyle(color: Colors.white, fontWeight: FontWeight.bold)),
              ),
            ),
            const SizedBox(height: 30),

            // Verified Reviews List
            const Text('Verified Traveler Reviews', style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold, color: AppTheme.primaryDark)),
            const SizedBox(height: 12),
            if (_isLoadingReviews)
              const Center(child: CircularProgressIndicator())
            else if (_reviews.isEmpty)
              const Text('No reviews yet for this tour.', style: TextStyle(color: AppTheme.textLight))
            else
              ListView.separated(
                shrinkWrap: true,
                physics: const NeverScrollableScrollPhysics(),
                itemCount: _reviews.length,
                separatorBuilder: (context, index) => const SizedBox(height: 10),
                itemBuilder: (context, index) {
                  final r = _reviews[index];
                  return Container(
                    padding: const EdgeInsets.all(14),
                    decoration: BoxDecoration(
                      color: Colors.white,
                      borderRadius: BorderRadius.circular(10),
                      border: Border.all(color: const Color(0xFFE2E8F0)),
                    ),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Row(
                          mainAxisAlignment: MainAxisAlignment.spaceBetween,
                          children: [
                            Text(r.userName ?? 'Verified Traveler', style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 13)),
                            Row(
                              children: List.generate(
                                r.rating,
                                (starIdx) => const Icon(Icons.star, size: 14, color: Color(0xFFF59E0B)),
                              ),
                            ),
                          ],
                        ),
                        const SizedBox(height: 6),
                        Text(r.comment, style: const TextStyle(fontSize: 13, color: Color(0xFF475569))),
                      ],
                    ),
                  );
                },
              ),
          ],
        ),
      ),
    );
  }
}
