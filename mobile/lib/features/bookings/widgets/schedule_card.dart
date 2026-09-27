import 'package:flutter/material.dart';
import 'package:intl/intl.dart';
import '../models/booking.dart';
import '../theme/booking_theme.dart';
import 'primary_gradient_button.dart';

class ScheduleCard extends StatelessWidget {
  final BookingSchedule schedule;
  final VoidCallback onSelect;

  const ScheduleCard({
    super.key,
    required this.schedule,
    required this.onSelect,
  });

  @override
  Widget build(BuildContext context) {
    final currencyFormatter = NumberFormat.currency(
      symbol: 'LKR ',
      decimalDigits: 0,
    );

    final nextDate = schedule.availableDates.isNotEmpty
        ? DateFormat('EEE, d MMM').format(schedule.availableDates.first)
        : 'Multiple dates';

    return Container(
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(22),
        border: Border.all(color: BookingTheme.border),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withValues(alpha: 0.04),
            blurRadius: 14,
            offset: const Offset(0, 5),
          ),
        ],
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          // Header / Banner gradient
          Container(
            padding: const EdgeInsets.all(18),
            decoration: const BoxDecoration(
              gradient: BookingTheme.heroGradient,
              borderRadius: BorderRadius.only(
                topLeft: Radius.circular(21),
                topRight: Radius.circular(21),
              ),
            ),
            child: Row(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        schedule.destinationTitle,
                        style: const TextStyle(
                          color: Colors.white,
                          fontSize: 18,
                          fontWeight: FontWeight.w800,
                          height: 1.2,
                        ),
                      ),
                      const SizedBox(height: 8),
                      Row(
                        children: [
                          const Icon(
                            Icons.location_on_outlined,
                            color: Colors.white70,
                            size: 15,
                          ),
                          const SizedBox(width: 4),
                          Expanded(
                            child: Text(
                              schedule.location,
                              style: const TextStyle(
                                color: Colors.white70,
                                fontSize: 13,
                              ),
                              overflow: TextOverflow.ellipsis,
                            ),
                          ),
                        ],
                      ),
                    ],
                  ),
                ),
                const SizedBox(width: 12),
                Container(
                  padding: const EdgeInsets.all(12),
                  decoration: BoxDecoration(
                    color: Colors.white.withValues(alpha: 0.2),
                    borderRadius: BorderRadius.circular(16),
                  ),
                  child: const Icon(
                    Icons.explore_rounded,
                    color: Colors.white,
                    size: 28,
                  ),
                ),
              ],
            ),
          ),

          // Body details
          Padding(
            padding: const EdgeInsets.all(18),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  children: [
                    // Guide chip
                    Container(
                      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 6),
                      decoration: BoxDecoration(
                        color: BookingTheme.background,
                        borderRadius: BorderRadius.circular(10),
                      ),
                      child: Row(
                        mainAxisSize: MainAxisSize.min,
                        children: [
                          const Icon(
                            Icons.person_pin_rounded,
                            size: 15,
                            color: BookingTheme.primary,
                          ),
                          const SizedBox(width: 6),
                          Text(
                            schedule.guideName,
                            style: const TextStyle(
                              fontSize: 12,
                              fontWeight: FontWeight.w600,
                              color: BookingTheme.forestDark,
                            ),
                          ),
                        ],
                      ),
                    ),
                    const Spacer(),
                    // Rating numeric badge (static numeric display only — Student 4 owns reviews)
                    Container(
                      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
                      decoration: BoxDecoration(
                        color: const Color(0xFFFFF9E6),
                        borderRadius: BorderRadius.circular(8),
                      ),
                      child: Row(
                        mainAxisSize: MainAxisSize.min,
                        children: [
                          const Icon(
                            Icons.star_rounded,
                            size: 15,
                            color: BookingTheme.goldStar,
                          ),
                          const SizedBox(width: 4),
                          Text(
                            '${schedule.rating.toStringAsFixed(1)} (${schedule.reviewsCount})',
                            style: const TextStyle(
                              fontSize: 12,
                              fontWeight: FontWeight.w700,
                              color: Color(0xFF8C6D00),
                            ),
                          ),
                        ],
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: 14),

                // Next Available Date & Slots Info
                Row(
                  children: [
                    const Icon(
                      Icons.event_available_rounded,
                      size: 16,
                      color: BookingTheme.textMuted,
                    ),
                    const SizedBox(width: 6),
                    Text(
                      'Next date: $nextDate',
                      style: const TextStyle(
                        fontSize: 13,
                        color: BookingTheme.textMuted,
                      ),
                    ),
                    const Spacer(),
                    Text(
                      '${schedule.availableTimeSlots.length} daily slots',
                      style: const TextStyle(
                        fontSize: 12,
                        fontWeight: FontWeight.w600,
                        color: BookingTheme.primary,
                      ),
                    ),
                  ],
                ),

                const Divider(height: 28, color: BookingTheme.border),

                // Price and Book button
                Row(
                  children: [
                    Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        const Text(
                          'Starting from',
                          style: TextStyle(
                            fontSize: 11,
                            color: BookingTheme.textMuted,
                          ),
                        ),
                        Text(
                          currencyFormatter.format(schedule.pricePerPerson),
                          style: const TextStyle(
                            fontSize: 18,
                            fontWeight: FontWeight.w900,
                            color: BookingTheme.forestDark,
                          ),
                        ),
                        const Text(
                          '/ person',
                          style: TextStyle(
                            fontSize: 11,
                            color: BookingTheme.textMuted,
                          ),
                        ),
                      ],
                    ),
                    const Spacer(),
                    PrimaryGradientButton(
                      height: 42,
                      onPressed: onSelect,
                      text: 'Select Slot',
                      textStyle: const TextStyle(
                        fontSize: 14,
                        fontWeight: FontWeight.w700,
                        color: Colors.white,
                      ),
                      trailingIcon: const Icon(Icons.arrow_forward_rounded, size: 16, color: Colors.white),
                    ),
                  ],
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}
