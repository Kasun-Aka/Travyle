import 'package:dio/dio.dart';
import 'package:flutter/foundation.dart';

String _resolveApiBaseUrl() {
  const envUrl = String.fromEnvironment('BOOKING_API_BASE_URL');
  if (envUrl.isNotEmpty) return envUrl;
  if (kIsWeb) return 'http://localhost:5085';
  if (defaultTargetPlatform == TargetPlatform.android) {
    return 'http://10.0.2.2:5085';
  }
  return 'http://localhost:5085';
}

class DestinationApiService {
  DestinationApiService()
    : _dio = Dio(
        BaseOptions(
          baseUrl: _resolveApiBaseUrl(),
          connectTimeout: const Duration(seconds: 10),
          receiveTimeout: const Duration(seconds: 15),
          headers: {
            'Content-Type': 'application/json',
            'Accept': 'application/json',
          },
        ),
      );

  final Dio _dio;

  Future<List<Map<String, dynamic>>> getDestinations() async {
    final response = await _dio.get(
      '/api/destinations',
      queryParameters: {'page': 1, 'pageSize': 100},
    );
    final body = Map<String, dynamic>.from(response.data as Map);
    return List<Map<String, dynamic>>.from(body['items'] as List);
  }
}

final destinationApiService = DestinationApiService();
