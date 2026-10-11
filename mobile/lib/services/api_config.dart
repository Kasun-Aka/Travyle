import 'package:flutter/foundation.dart';

/// Centralized API configuration and base URL resolver for the Travyle mobile app.
/// Ensures mobile APK builds automatically connect to the deployed production backend,
/// while supporting local debug emulator/web workflows and --dart-define overrides.
class ApiConfig {
  static const String productionUrl = 'https://travyle-production.up.railway.app';

  /// Resolves the base backend origin without a trailing slash (e.g., 'https://travyle-production.up.railway.app' or 'http://10.0.2.2:5085').
  static String get baseUrl {
    const String envUrl = String.fromEnvironment('API_BASE_URL');
    if (envUrl.isNotEmpty) {
      return envUrl.endsWith('/') ? envUrl.substring(0, envUrl.length - 1) : envUrl;
    }

    const String bookingEnvUrl = String.fromEnvironment('BOOKING_API_BASE_URL');
    if (bookingEnvUrl.isNotEmpty) {
      return bookingEnvUrl.endsWith('/') ? bookingEnvUrl.substring(0, bookingEnvUrl.length - 1) : bookingEnvUrl;
    }

    // Release mode (such as release mobile APK) defaults to deployed backend
    if (kReleaseMode) {
      return productionUrl;
    }

    // Debug mode defaults:
    if (kIsWeb) {
      return 'http://localhost:5085';
    }
    if (defaultTargetPlatform == TargetPlatform.android) {
      return 'http://10.0.2.2:5085';
    }
    return 'http://localhost:5085';
  }

  /// Resolves the API prefix (e.g., 'https://travyle-production.up.railway.app/api' or 'http://10.0.2.2:5085/api').
  static String get apiBaseUrl => '$baseUrl/api';
}
