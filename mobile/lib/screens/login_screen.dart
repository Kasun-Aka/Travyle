import 'package:flutter/material.dart';
import 'package:flutter/foundation.dart';
import 'package:firebase_auth/firebase_auth.dart';
import 'package:dio/dio.dart';
import '../theme/app_theme.dart';
import 'home_screen.dart';
import 'reset_password_screen.dart';
import 'signup_screen.dart';

class LoginScreen extends StatefulWidget {
  const LoginScreen({super.key});

  @override
  State<LoginScreen> createState() => _LoginScreenState();
}

class _LoginScreenState extends State<LoginScreen> {
  final _emailController = TextEditingController();
  final _passwordController = TextEditingController();
  bool _obscurePassword = true;
  bool _isLoading = false;
  String? _errorMessage;

  @override
  void dispose() {
    _emailController.dispose();
    _passwordController.dispose();
    super.dispose();
  }

  Future<void> _handleLogin() async {
    final email = _emailController.text.trim();
    final password = _passwordController.text;
    if (email.isEmpty || password.isEmpty) {
      setState(() => _errorMessage = 'Enter your email and password.');
      return;
    }

    setState(() {
      _isLoading = true;
      _errorMessage = null;
    });
    try {
      final credential = await FirebaseAuth.instance.signInWithEmailAndPassword(
        email: email,
        password: password,
      );
      final user = credential.user;
      if (user == null) throw Exception('Unable to load the signed-in account.');

      
      if (user != null) {
          try {
            final dio = Dio();
            final baseUrl = kIsWeb ? 'http://localhost:5085' : 'http://10.0.2.2:5085';
            final token = await user.getIdToken();
            final options = Options(headers: {'Authorization': 'Bearer $token'});
            // Fetch existing user data to preserve their role
            try {
              await dio.get('$baseUrl/api/auth/user', queryParameters: {'email': email}, options: options);
            } catch (_) {
              // User doesn't exist in DB yet - sync to create them
              await dio.post('$baseUrl/api/auth/sync', data: {
                'firebaseUid': user.uid,
                'email': email,
                'fullName': user.displayName ?? '',
                'role': 'Traveler',
              }, options: options);
            }
          } catch (apiError) {
            debugPrint('Failed to fetch/sync user with DB on login: $apiError');
          }
        }

