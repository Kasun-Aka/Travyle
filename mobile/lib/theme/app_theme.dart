import 'package:flutter/material.dart';

class AppTheme {
  static const Color primaryDark = Color(0xFF0F3E4C);
  static const Color coral = Color(0xFFD18D74);
  static const Color lightGrey = Color(0xFFF9F9F9);
  static const Color textDark = Color(0xFF1E293B);
  static const Color textLight = Color(0xFF94A3B8);

  static ThemeData get theme {
    return ThemeData(
      primaryColor: primaryDark,
      scaffoldBackgroundColor: const Color(0xFFFBFBFB),
      fontFamily: 'Inter', // Assuming Inter or similar sans-serif
      colorScheme: ColorScheme.fromSeed(
        seedColor: primaryDark,
        primary: primaryDark,
      ),
      inputDecorationTheme: InputDecorationTheme(
        filled: true,
        fillColor: Colors.white,
        contentPadding: const EdgeInsets.symmetric(horizontal: 20, vertical: 16),
        border: OutlineInputBorder(
          borderRadius: BorderRadius.circular(30),
          borderSide: const BorderSide(color: Color(0xFFE2E8F0)),
        ),
        enabledBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(30),
          borderSide: const BorderSide(color: Color(0xFFE2E8F0)),
        ),
        focusedBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(30),
          borderSide: const BorderSide(color: primaryDark, width: 2),
        ),
        hintStyle: const TextStyle(color: textLight, fontSize: 16),
      ),
      textTheme: const TextTheme(
        headlineMedium: TextStyle(
          color: primaryDark,
          fontWeight: FontWeight.w800,
          fontSize: 32,
        ),
        bodyLarge: TextStyle(
          color: textDark,
          fontSize: 16,
        ),
        bodyMedium: TextStyle(
          color: textLight,
          fontSize: 14,
        ),
      ),
      elevatedButtonTheme: ElevatedButtonThemeData(
        style: ElevatedButton.styleFrom(
          backgroundColor: Colors.transparent, // Handled by gradient
          shadowColor: Colors.transparent,
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(30),
          ),
        ),
      ),
    );
  }
}
