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
          LayoutBuilder(
            builder: (context, constraints) {
              final compact = constraints.maxWidth < 340;
              final guideChip = ConstrainedBox(
                constraints: BoxConstraints(maxWidth: constraints.maxWidth),
                child: Container(
                  padding: const EdgeInsets.symmetric(
                    horizontal: 10,
                    vertical: 6,
                  ),
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
                      Flexible(
                        child: Text(
                          schedule.guideName,
                          maxLines: 1,
                          overflow: TextOverflow.ellipsis,
                          style: const TextStyle(
                            fontSize: 12,
                            fontWeight: FontWeight.w600,
                            color: BookingTheme.forestDark,
                          ),
                        ),
                      ),
                    ],
                  ),
                ),
              );
              final ratingBadge = Container(
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
              );
              final dateDetails = Row(
                children: [
                  const Icon(
                    Icons.event_available_rounded,
                    size: 16,
                    color: BookingTheme.textMuted,
                  ),
                  const SizedBox(width: 6),
                  Expanded(
                    child: Text(
                      'Next date: $nextDate',
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                      style: const TextStyle(
                        fontSize: 13,
                        color: BookingTheme.textMuted,
                      ),
                    ),
                  ),
                  if (!compact) ...[
                    const SizedBox(width: 8),
                    Text(
                      '${schedule.availableTimeSlots.length} daily slots',
                      style: const TextStyle(
                        fontSize: 12,
                        fontWeight: FontWeight.w600,
                        color: BookingTheme.primary,
                      ),
                    ),
                  ],
                ],
              );
              final price = Column(
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
              );
              final selectButton = PrimaryGradientButton(
                height: 42,
                width: compact ? double.infinity : null,
                onPressed: onSelect,
                text: 'Select Slot',
                textStyle: const TextStyle(
                  fontSize: 14,
                  fontWeight: FontWeight.w700,
                  color: Colors.white,
                ),
                trailingIcon: const Icon(
                  Icons.arrow_forward_rounded,
                  size: 16,
                  color: Colors.white,
                ),
              );

              return Padding(
                padding: EdgeInsets.all(compact ? 14 : 18),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    if (compact)
                      Wrap(
                        spacing: 8,
                        runSpacing: 8,
                        crossAxisAlignment: WrapCrossAlignment.center,
                        children: [guideChip, ratingBadge],
                      )
                    else
                      Row(
                        children: [
                          Expanded(child: guideChip),
                          const SizedBox(width: 8),
                          ratingBadge,
                        ],
                      ),
                    const SizedBox(height: 14),
                    dateDetails,
                    if (compact) ...[
                      const SizedBox(height: 6),
                      Text(
                        '${schedule.availableTimeSlots.length} daily slots',
                        style: const TextStyle(
                          fontSize: 12,
                          fontWeight: FontWeight.w600,
                          color: BookingTheme.primary,
                        ),
                      ),
                    ],
                    const Divider(height: 28, color: BookingTheme.border),
                    if (compact) ...[
                      price,
                      const SizedBox(height: 12),
                      selectButton,
                    ] else
                      Row(children: [price, const Spacer(), selectButton]),
                  ],
                ),
              );
            },
          ),
        ],
      ),
    );
  }
}
