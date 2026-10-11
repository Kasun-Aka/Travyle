import 'package:dio/dio.dart';
import '../../../../services/api_config.dart';

String _resolveApiBaseUrl() {
  return ApiConfig.baseUrl;
}

class DestinationApiService {
  DestinationApiService()
    : _dio = Dio(
        BaseOptions(
          baseUrl: _resolveApiBaseUrl(),
          connectTimeout: const Duration(seconds: 50),
          receiveTimeout: const Duration(seconds: 50),
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
