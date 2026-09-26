// Theme based on Travyle palette: primary copper #D4764E, deep navy #0F2B38, spruce teal #1A3A4A, warm cream #F5F3F0.
// Keeps mobile and web visually consistent across the application.

import 'package:flutter/material.dart';

class BookingTheme {
  static const Color primary = Color(0xFFD4764E);
  static const Color primaryDark = Color(0xFFB55E3A);
  static const Color forestDark = Color(0xFF0F2B38);
  static const Color spruce = Color(0xFF1A3A4A);
  static const Color background = Color(0xFFF5F3F0);
  static const Color cardBg = Colors.white;
  static const Color mintLight = Color(0xFFEBF0F2);
  static const Color border = Color(0xFFE0DCD8);
  static const Color textMuted = Color(0xFF7A7570);
  static const Color goldStar = Color(0xFFFFD166);
  static const Color warningOrange = Color(0xFFF39C12);
  static const Color errorRed = Color(0xFFE53935);
  static const Color infoBlue = Color(0xFF2980B9);

  static const LinearGradient heroGradient = LinearGradient(
    colors: [Color(0xFF1A3A4A), Color(0xFF2B5A6A)],
    begin: Alignment.topLeft,
    end: Alignment.bottomRight,
  );

  static const LinearGradient buttonGradient = LinearGradient(
    colors: [primary, spruce],
    begin: Alignment.centerLeft,
    end: Alignment.centerRight,
  );

  static ThemeData get themeData {
    return ThemeData(
      useMaterial3: true,
      scaffoldBackgroundColor: background,
      colorScheme: ColorScheme.fromSeed(
        seedColor: primary,
        primary: primary,
        onPrimary: Colors.white,
        surface: cardBg,
        brightness: Brightness.light,
      ),
      appBarTheme: const AppBarTheme(
        backgroundColor: background,
        elevation: 0,
        centerTitle: false,
        iconTheme: IconThemeData(color: forestDark),
        titleTextStyle: TextStyle(
          color: forestDark,
          fontSize: 20,
          fontWeight: FontWeight.w800,
        ),
      ),
      elevatedButtonTheme: ElevatedButtonThemeData(
        style: ElevatedButton.styleFrom(
          backgroundColor: primary,
          foregroundColor: Colors.white,
          elevation: 0,
          padding: const EdgeInsets.symmetric(horizontal: 24, vertical: 16),
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(16),
          ),
          textStyle: const TextStyle(
            fontSize: 16,
            fontWeight: FontWeight.w700,
          ),
        ),
      ),
      inputDecorationTheme: InputDecorationTheme(
        filled: true,
        fillColor: Colors.white,
        contentPadding: const EdgeInsets.symmetric(horizontal: 16, vertical: 14),
        border: OutlineInputBorder(
          borderRadius: BorderRadius.circular(14),
          borderSide: const BorderSide(color: border),
        ),
        enabledBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(14),
          borderSide: const BorderSide(color: border),
        ),
        focusedBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(14),
          borderSide: const BorderSide(color: primary, width: 2),
        ),
        errorBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(14),
          borderSide: const BorderSide(color: errorRed),
        ),
      ),
    );
  }
}
