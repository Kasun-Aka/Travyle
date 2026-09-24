import 'package:dio/dio.dart';
import 'package:flutter/foundation.dart';
import '../models/support_ticket.dart';
import '../models/voucher.dart';
import '../models/customer_review.dart';

class SupportApiService {
  static String get baseUrl {
    if (kIsWeb) {
      return 'http://localhost:5000/api';
    }
    if (defaultTargetPlatform == TargetPlatform.android) {
      return 'http://10.0.2.2:5000/api';
    }
    return 'http://localhost:5000/api';
  }

  final Dio _dio;

  SupportApiService({Dio? dio})
      : _dio = dio ??
            Dio(BaseOptions(
              baseUrl: baseUrl,
              connectTimeout: const Duration(seconds: 10),
              receiveTimeout: const Duration(seconds: 10),
              headers: {'Content-Type': 'application/json'},
            ));

  // Default Demo Traveler ID
  static const String defaultTravelerId = '11111111-1111-1111-1111-111111111111';

  // Support Tickets
  Future<List<SupportTicketModel>> getTickets({String? priority, String? status, String? search}) async {
    try {
      final response = await _dio.get('/support/tickets', queryParameters: {
        if (priority != null && priority != 'ALL') 'priority': priority,
        if (status != null && status != 'ALL') 'status': status,
        if (search != null && search.isNotEmpty) 'search': search,
        'pageSize': 50,
      });

      if (response.statusCode == 200 && response.data != null) {
        final List<dynamic> items = response.data['items'] ?? [];
        return items.map((e) => SupportTicketModel.fromJson(e as Map<String, dynamic>)).toList();
      }
      return [];
    } catch (e) {
      debugPrint('[SupportApiService] Error fetching tickets: $e');
      return [];
    }
  }

  Future<SupportTicketModel?> getTicketById(String id) async {
    try {
      final response = await _dio.get('/support/tickets/$id');
      if (response.statusCode == 200 && response.data != null) {
        return SupportTicketModel.fromJson(response.data as Map<String, dynamic>);
      }
      return null;
    } catch (e) {
      debugPrint('[SupportApiService] Error fetching ticket by id: $e');
      return null;
    }
  }

  Future<SupportTicketModel?> createTicket({
    required String title,
    required String description,
    required String category,
    required String priority,
    String? attachmentUrl,
    String? bookingId,
    String? tourId,
    String? userId,
  }) async {
    try {
      final response = await _dio.post('/support/tickets', data: {
        'userId': userId ?? defaultTravelerId,
        'title': title,
        'description': description,
        'category': category,
        'priority': priority,
        'attachmentUrl': attachmentUrl,
        'bookingId': bookingId,
        'tourId': tourId,
      });

      if (response.statusCode == 200 || response.statusCode == 201) {
        return SupportTicketModel.fromJson(response.data as Map<String, dynamic>);
      }
      return null;
    } catch (e) {
      debugPrint('[SupportApiService] Error creating ticket: $e');
      return null;
    }
  }

  Future<bool> autoResolveClaim(String id) async {
    try {
      final response = await _dio.post('/support/tickets/$id/auto-resolve-claim');
      return response.statusCode == 200;
    } catch (e) {
      debugPrint('[SupportApiService] Error running auto-resolve: $e');
      return false;
    }
  }

  // Digital Vouchers
  Future<List<VoucherModel>> getUserVouchers({String? userId}) async {
    final uid = userId ?? defaultTravelerId;
    try {
      final response = await _dio.get('/support/vouchers/user/$uid');
      if (response.statusCode == 200 && response.data != null) {
        final List<dynamic> items = response.data as List<dynamic>;
        return items.map((e) => VoucherModel.fromJson(e as Map<String, dynamic>)).toList();
      }
      return [];
    } catch (e) {
      debugPrint('[SupportApiService] Error fetching vouchers: $e');
      return [];
    }
  }

  // Customer Reviews
  Future<List<CustomerReviewModel>> getReviewsByTour(String tourId) async {
    try {
      final response = await _dio.get('/support/reviews/$tourId');
      if (response.statusCode == 200 && response.data != null) {
        final List<dynamic> items = response.data as List<dynamic>;
        return items.map((e) => CustomerReviewModel.fromJson(e as Map<String, dynamic>)).toList();
      }
      return [];
    } catch (e) {
      debugPrint('[SupportApiService] Error fetching tour reviews: $e');
      return [];
    }
  }

  Future<CustomerReviewModel?> createReview({
    required String tourId,
    required int rating,
    required String comment,
    String? userId,
  }) async {
    try {
      final response = await _dio.post('/support/reviews', data: {
        'userId': userId ?? defaultTravelerId,
        'tourId': tourId,
        'rating': rating,
        'comment': comment,
      });

      if (response.statusCode == 200 || response.statusCode == 201) {
        return CustomerReviewModel.fromJson(response.data as Map<String, dynamic>);
      }
      return null;
    } catch (e) {
      debugPrint('[SupportApiService] Error creating review: $e');
      return null;
    }
  }
}
