import 'package:dio/dio.dart';
import 'package:firebase_auth/firebase_auth.dart';
import 'package:flutter/foundation.dart';
import '../models/support_ticket.dart';
import '../models/voucher.dart';
import '../models/customer_review.dart';

class SupportApiService {
  static String? currentUserId;

  static String get baseUrl {
    const String envUrl = String.fromEnvironment('API_BASE_URL');
    if (envUrl.isNotEmpty) return '$envUrl/api';
    if (kIsWeb) {
      return 'http://localhost:5085';
    }
    if (defaultTargetPlatform == TargetPlatform.android) {
      return 'http://10.0.2.2:5085';
    }
    return 'http://localhost:5085';
  }

  final Dio _dio;

  /// Attaches the signed-in user's Firebase ID token to every request.
  static Dio _withAuth(Dio dio) {
    dio.interceptors.add(InterceptorsWrapper(
      onRequest: (options, handler) async {
        try {
          final token = await FirebaseAuth.instance.currentUser?.getIdToken();
          if (token != null && token.isNotEmpty) {
            options.headers['Authorization'] = 'Bearer $token';
          }
        } catch (e) {
          debugPrint('[SupportApiService] Could not read Firebase token: $e');
        }
        handler.next(options);
      },
    ));
    return dio;
  }

  SupportApiService({Dio? dio})
      : _dio = dio ??
            _withAuth(Dio(BaseOptions(
              baseUrl: baseUrl,
              connectTimeout: const Duration(seconds: 50),
              receiveTimeout: const Duration(seconds: 50),
              headers: {'Content-Type': 'application/json'},
            )));

  // Default Demo Traveler ID
  static const String defaultTravelerId = '11111111-1111-1111-1111-111111111111';

  /// Resolves real app-level user ID (Users.Id) for signed-in Firebase traveler.
  Future<String?> getCurrentUserId() async {
    if (currentUserId != null && currentUserId!.isNotEmpty) {
      return currentUserId;
    }
    try {
      final user = FirebaseAuth.instance.currentUser;
      if (user != null && user.email != null) {
        final idToken = await user.getIdToken();
        final response = await _dio.get(
          '/auth/user',
          queryParameters: {'email': user.email},
          options: Options(headers: {'Authorization': 'Bearer $idToken'}),
        );
        if (response.statusCode == 200 && response.data != null) {
          final id = (response.data['id'] ?? response.data['Id'])?.toString();
          if (id != null && id.isNotEmpty) {
            currentUserId = id;
            return id;
          }
        }
      }
    } catch (e) {
      debugPrint('[SupportApiService] Error fetching current user ID with token: $e');
      try {
        final user = FirebaseAuth.instance.currentUser;
        if (user?.email != null) {
          final response = await _dio.get('/auth/user', queryParameters: {'email': user!.email});
          if (response.statusCode == 200 && response.data != null) {
            final id = (response.data['id'] ?? response.data['Id'])?.toString();
            if (id != null && id.isNotEmpty) {
              currentUserId = id;
              return id;
            }
          }
        }
      } catch (fallbackErr) {
        debugPrint('[SupportApiService] Fallback user fetch failed: $fallbackErr');
      }
    }
    return null;
  }

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
      debugPrint('[SupportApiService] Error fetching tickets ($baseUrl): $e');
      try {
        final fallbackHost = baseUrl.replaceAll(':5085', ':5000');
        final fallbackDio = _withAuth(Dio(BaseOptions(baseUrl: fallbackHost, connectTimeout: const Duration(seconds: 5))));
        final response = await fallbackDio.get('/support/tickets', queryParameters: {
          if (priority != null && priority != 'ALL') 'priority': priority,
          if (status != null && status != 'ALL') 'status': status,
          if (search != null && search.isNotEmpty) 'search': search,
          'pageSize': 50,
        });
        if (response.statusCode == 200 && response.data != null) {
          final List<dynamic> items = response.data['items'] ?? [];
          return items.map((e) => SupportTicketModel.fromJson(e as Map<String, dynamic>)).toList();
        }
      } catch (fallbackError) {
        debugPrint('[SupportApiService] Fallback getTickets failed: $fallbackError');
      }
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
    final payload = {
      'userId': userId ?? defaultTravelerId,
      'title': title,
      'description': description,
      'category': category,
      'priority': priority,
      'attachmentUrl': attachmentUrl,
      'bookingId': bookingId,
      'tourId': tourId,
    };

    try {
      final response = await _dio.post('/support/tickets', data: payload);

      if (response.statusCode == 200 || response.statusCode == 201) {
        return SupportTicketModel.fromJson(response.data as Map<String, dynamic>);
      }
      return null;
    } catch (e) {
      debugPrint('[SupportApiService] Error creating ticket on primary URL ($baseUrl): $e');
      if (e is DioException) {
        debugPrint('[SupportApiService] Response: ${e.response?.statusCode} -> ${e.response?.data}');
      }

      // Fallback: If primary port (5085) fails, attempt secondary standard dev port (5000)
      try {
        final fallbackHost = baseUrl.replaceAll(':5085', ':5000');
        debugPrint('[SupportApiService] Attempting fallback endpoint: $fallbackHost/support/tickets');
        final fallbackDio = _withAuth(Dio(BaseOptions(
          baseUrl: fallbackHost,
          connectTimeout: const Duration(seconds: 5),
          receiveTimeout: const Duration(seconds: 5),
          headers: {'Content-Type': 'application/json'},
        )));
        final response = await fallbackDio.post('/support/tickets', data: payload);
        if (response.statusCode == 200 || response.statusCode == 201) {
          return SupportTicketModel.fromJson(response.data as Map<String, dynamic>);
        }
      } catch (fallbackError) {
        debugPrint('[SupportApiService] Fallback connection failed: $fallbackError');
      }

      return null;
    }
  }

  Future<String?> uploadTicketAttachment(Uint8List imageBytes, String filename) async {
    try {
      final formData = FormData.fromMap({
        'file': MultipartFile.fromBytes(imageBytes, filename: filename),
      });
      final response = await _dio.post('/support/tickets/upload-attachment', data: formData);
      if (response.statusCode == 200 && response.data != null) {
        return response.data['url'] as String?;
      }
      return null;
    } catch (e) {
      debugPrint('[SupportApiService] Error uploading attachment: $e');
      try {
        final fallbackHost = baseUrl.replaceAll(':5085', ':5000');
        final fallbackDio = _withAuth(Dio(BaseOptions(baseUrl: fallbackHost, connectTimeout: const Duration(seconds: 5))));
        final formData = FormData.fromMap({
          'file': MultipartFile.fromBytes(imageBytes, filename: filename),
        });
        final response = await fallbackDio.post('/support/tickets/upload-attachment', data: formData);
        if (response.statusCode == 200 && response.data != null) {
          return response.data['url'] as String?;
        }
      } catch (fallbackError) {
        debugPrint('[SupportApiService] Fallback uploadAttachment failed: $fallbackError');
      }
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

  /// Fetches available destinations/tours from the backend API for reviews selection.
  Future<List<Map<String, String>>> getAvailableTours() async {
    final List<Map<String, String>> tours = [
      {
        'id': '33333333-3333-3333-3333-333333333333',
        'title': 'Alpine Excursion & Scenic Rail Tour',
      },
      {
        'id': '11111111-1111-1111-1111-111111111111',
        'title': 'Ella Gap & Nine Arch Bridge Trek',
      },
      {
        'id': '22222222-2222-2222-2222-222222222222',
        'title': 'Sigiriya Fortress & Minneriya Wildlife Safari',
      },
      {
        'id': '44444444-4444-4444-4444-444444444444',
        'title': 'Galle Fort & Southern Coast Coastal Odyssey',
      },
    ];

    try {
      final response = await _dio.get('/destinations');
      if (response.statusCode == 200 && response.data != null) {
        final data = response.data;
        List<dynamic>? items;
        if (data is Map && data.containsKey('items')) {
          items = data['items'] as List<dynamic>?;
        } else if (data is List) {
          items = data;
        }

        if (items != null) {
          for (var item in items) {
            final id = item['id']?.toString();
            final name = item['name']?.toString();
            if (id != null && name != null && !tours.any((t) => t['id'] == id)) {
              tours.add({'id': id, 'title': name});
            }
          }
        }
      }
    } catch (e) {
      debugPrint('[SupportApiService] Fetch destinations for review fallback: $e');
    }
    return tours;
  }

  Future<SupportTicketModel?> cancelTicket(String id) async {
    try {
      final response = await _dio.post('/support/tickets/$id/cancel');
      if (response.statusCode == 200 && response.data != null) {
        return SupportTicketModel.fromJson(response.data as Map<String, dynamic>);
      }
      return null;
    } catch (e) {
      debugPrint('[SupportApiService] Error cancelling ticket: $e');
      return null;
    }
  }

  Future<SupportTicketModel?> addFollowupNote(String id, String note) async {
    try {
      final response = await _dio.post(
        '/support/tickets/$id/followup',
        data: {'note': note},
      );
      if (response.statusCode == 200 && response.data != null) {
        return SupportTicketModel.fromJson(response.data as Map<String, dynamic>);
      }
      return null;
    } catch (e) {
      debugPrint('[SupportApiService] Error adding follow-up note: $e');
      return null;
    }
  }
}
