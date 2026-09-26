import 'package:flutter/material.dart';
import '../theme/booking_theme.dart';

class LoadingState extends StatelessWidget {
  final String message;
  const LoadingState({super.key, this.message = 'Loading available schedules...'});

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Column(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          const CircularProgressIndicator(
            color: BookingTheme.primary,
            strokeWidth: 3,
          ),
          const SizedBox(height: 16),
          Text(
            message,
            style: const TextStyle(
              color: BookingTheme.textMuted,
              fontSize: 14,
              fontWeight: FontWeight.w500,
            ),
          ),
        ],
      ),
    );
  }
}
