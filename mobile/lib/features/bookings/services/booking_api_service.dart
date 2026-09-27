// ============================================================================
// BookingApiService — wraps all 8 ASP.NET Core booking endpoints using Dio.
//
// Base URL is configurable via the kBookingApiBaseUrl constant so it can be
// changed for local dev (Android emulator, iOS simulator, or web) without
// touching provider code.
//
// Endpoints covered:
//   GET    /api/booking-schedules                        → getSchedules
//   GET    /api/booking-schedules/{id}                   → getScheduleById
//   POST   /api/bookings                                 → createBooking
//   GET    /api/bookings/traveler/{travelerId}           → getTravelerBookings
//   GET    /api/bookings/{id}                            → getBookingById
//   PUT    /api/bookings/{id}/status                     → updateBookingStatus
//   DELETE /api/bookings/{id}                            → cancelBooking
//   POST   /api/bookings/{id}/process-escrow-payment     → processEscrowPayment
//   POST   /api/discount-requests                        → createDiscountRequest
// ============================================================================

import 'package:dio/dio.dart';
import 'package:firebase_auth/firebase_auth.dart';
import 'package:flutter/foundation.dart';

// ── Base URL config ──────────────────────────────────────────────────────────
// • Android emulator  → 10.0.2.2
// • iOS simulator     → 127.0.0.1
// • Web (Flutter web) → localhost
// • Physical device   → your machine's LAN IP (e.g. 192.168.x.x)
String _resolveBookingApiBaseUrl() {
  const envUrl = String.fromEnvironment('BOOKING_API_BASE_URL');
  if (envUrl.isNotEmpty) return envUrl;

  if (kIsWeb) return 'http://localhost:5085';
  if (defaultTargetPlatform == TargetPlatform.android) {
    return 'http://10.0.2.2:5085';
  }
  // Windows desktop, macOS, Linux, iOS simulator
  return 'http://localhost:5085';
}

final String kBookingApiBaseUrl = _resolveBookingApiBaseUrl();

class BookingApiService {
  late final Dio _dio;

  BookingApiService() {
    _dio = Dio(
      BaseOptions(
        baseUrl: kBookingApiBaseUrl,
        connectTimeout: const Duration(seconds: 10),
        receiveTimeout: const Duration(seconds: 15),
        headers: {
          'Content-Type': 'application/json',
          'Accept': 'application/json',
        },
      ),
    );
    _dio.interceptors.add(
      InterceptorsWrapper(
        onRequest: (options, handler) async {
          try {
            final user = FirebaseAuth.instance.currentUser;
            final token = await user?.getIdToken();
            if (token != null) {
              options.headers['Authorization'] = 'Bearer $token';
            }
            handler.next(options);
          } catch (error, stackTrace) {
            handler.reject(
              DioException(
                requestOptions: options,
                error: error,
                stackTrace: stackTrace,
              ),
            );
          }
        },
      ),
    );

    if (kDebugMode) {
      _dio.interceptors.add(
        LogInterceptor(
          requestBody: true,
          responseBody: true,
          logPrint: (obj) => debugPrint('[BookingAPI] $obj'),
        ),
      );
    }
  }

  Future<Map<String, dynamic>> getAuthenticatedTraveler() async {
    final user = FirebaseAuth.instance.currentUser;
    if (user?.email == null) {
      throw StateError('Please sign in to use Smart Booking.');
    }

    final response = await _dio.get(
      '/api/auth/user',
      queryParameters: {'email': user!.email},
    );
    return Map<String, dynamic>.from(response.data as Map);
  }

  // ── 1. GET /api/booking-schedules ─────────────────────────────────────────
  Future<List<Map<String, dynamic>>> getSchedules({
    String? search,
    DateTime? date,
  }) async {
    final queryParams = <String, dynamic>{};
    if (search != null && search.isNotEmpty) queryParams['search'] = search;
    if (date != null) queryParams['date'] = date.toIso8601String();

    final response = await _dio.get(
      '/api/booking-schedules',
      queryParameters: queryParams,
    );
    return List<Map<String, dynamic>>.from(response.data as List);
  }

  // ── 2. GET /api/booking-schedules/{id} ───────────────────────────────────
  Future<Map<String, dynamic>> getScheduleById(String id) async {
    final response = await _dio.get('/api/booking-schedules/$id');
    return Map<String, dynamic>.from(response.data as Map);
  }

  // ── 3. POST /api/bookings ─────────────────────────────────────────────────
  Future<Map<String, dynamic>> createBooking({
    required String scheduleId,
    required String travelerId,
    required String travelerName,
    required String travelerEmail,
    required DateTime bookingDate,
    required String timeSlot,
    required int guests,
    String? notes,
    required String paymentMethod,
    String? receiptReference,
    String? receiptImageData,
  }) async {
    final response = await _dio.post(
      '/api/bookings',
      data: {
        'scheduleId': scheduleId,
        'travelerId': travelerId,
        'travelerName': travelerName,
        'travelerEmail': travelerEmail,
        'bookingDate': bookingDate.toIso8601String(),
        'timeSlot': timeSlot,
        'guests': guests,
        'notes': notes,
        'paymentMethod': paymentMethod,
        'receiptReference': receiptReference,
        'receiptImageData': receiptImageData,
      },
    );
    return Map<String, dynamic>.from(response.data as Map);
  }

  // ── 4. GET /api/bookings/traveler/{travelerId} ────────────────────────────
  Future<List<Map<String, dynamic>>> getTravelerBookings(
    String travelerId, {
    int page = 1,
    int pageSize = 20,
  }) async {
    final response = await _dio.get(
      '/api/bookings/traveler/$travelerId',
      queryParameters: {'page': page, 'pageSize': pageSize},
    );
    return List<Map<String, dynamic>>.from(response.data as List);
  }

  // ── 5. GET /api/bookings/{id} ─────────────────────────────────────────────
  Future<Map<String, dynamic>?> getBookingById(String id) async {
    try {
      final response = await _dio.get('/api/bookings/$id');
      return Map<String, dynamic>.from(response.data as Map);
    } on DioException catch (e) {
      if (e.response?.statusCode == 404) return null;
      rethrow;
    }
  }

  // ── 6. PUT /api/bookings/{id}/status ─────────────────────────────────────
  Future<Map<String, dynamic>?> updateBookingStatus(
    String id,
    String status,
  ) async {
    try {
      final response = await _dio.put(
        '/api/bookings/$id/status',
        data: {'status': status},
      );
      return Map<String, dynamic>.from(response.data as Map);
    } on DioException catch (e) {
      if (e.response?.statusCode == 404) return null;
      rethrow;
    }
  }

  // ── 7. DELETE /api/bookings/{id} ──────────────────────────────────────────
  Future<bool> cancelBooking(String id) async {
    try {
      await _dio.delete('/api/bookings/$id');
      return true;
    } on DioException catch (e) {
      if (e.response?.statusCode == 404) return false;
      rethrow;
    }
  }

  // ── 8. POST /api/bookings/{id}/process-escrow-payment ────────────────────
  Future<Map<String, dynamic>> processEscrowPayment({
    required String bookingId,
    required double totalAmount,
    required String paymentMethodId,
  }) async {
    final response = await _dio.post(
      '/api/bookings/$bookingId/process-escrow-payment',
      data: {
        'bookingId': bookingId,
        'totalAmount': totalAmount,
        'paymentMethodId': paymentMethodId,
      },
    );
    return Map<String, dynamic>.from(response.data as Map);
  }

  // ── 9. POST /api/discount-requests ───────────────────────────────────────
  Future<Map<String, dynamic>> createDiscountRequest({
    required String bookingId,
    required String scheduleId,
    required String travelerId,
    required double originalPrice,
    required double requestedDiscountPercent,
    required String reason,
  }) async {
    final response = await _dio.post(
      '/api/discount-requests',
      data: {
        'bookingId': bookingId,
        'scheduleId': scheduleId,
        'travelerId': travelerId,
        'originalPrice': originalPrice,
        'requestedDiscountPercent': requestedDiscountPercent,
        'reason': reason,
      },
    );
    return Map<String, dynamic>.from(response.data as Map);
  }

  // ── 10. GET /api/discount-requests/traveler/{travelerId} ──────────────────
  Future<List<Map<String, dynamic>>> getTravelerDiscountRequests(
    String travelerId,
  ) async {
    final response = await _dio.get(
      '/api/discount-requests/traveler/$travelerId',
    );
    return (response.data as List)
        .map((item) => Map<String, dynamic>.from(item as Map))
        .toList();
  }

  // ── 11. POST /api/agent/bookings/start ────────────────────────────────────
  Future<Map<String, dynamic>> startAgentBooking({
    required String travelerId,
    required String objective,
    String? travelerName,
    String? travelerEmail,
    String? preferredScheduleId,
    DateTime? preferredDate,
    String? preferredTimeSlot,
    int? guests,
  }) async {
    final response = await _dio.post(
      '/api/agent/bookings/start',
      data: {
        'travelerId': travelerId,
        'objective': objective,
        'travelerName': travelerName,
        'travelerEmail': travelerEmail,
        'preferredScheduleId': preferredScheduleId,
        'preferredDate': preferredDate?.toIso8601String(),
        'preferredTimeSlot': preferredTimeSlot,
        'guests': guests,
      },
    );
    return Map<String, dynamic>.from(response.data as Map);
  }

  // ── 12. GET /api/agent/workflows/{workflowId} ─────────────────────────────
  Future<Map<String, dynamic>> getAgentWorkflow(String workflowId) async {
    final response = await _dio.get('/api/agent/workflows/$workflowId');
    return Map<String, dynamic>.from(response.data as Map);
  }

  // ── 13. GET /api/agent/workflows/traveler/{travelerId} ────────────────────
  Future<List<Map<String, dynamic>>> getTravelerAgentWorkflows(
    String travelerId,
  ) async {
    final response = await _dio.get('/api/agent/workflows/traveler/$travelerId');
    return (response.data as List)
        .map((item) => Map<String, dynamic>.from(item as Map))
        .toList();
  }
}

// Singleton instance — shared across all providers
final bookingApiService = BookingApiService();
