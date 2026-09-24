import 'package:flutter/material.dart';
import 'screens/splash_screen.dart';
import 'theme/app_theme.dart';

void main() {
  runApp(const MyApp());
}

class TravyleApp extends StatelessWidget {
  const TravyleApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'Travyle',
      debugShowCheckedModeBanner: false,
      theme: AppTheme.theme,
      home: const SplashScreen(),
    );
  }
}
