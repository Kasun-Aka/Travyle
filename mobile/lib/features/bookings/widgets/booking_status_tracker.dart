import 'package:flutter/material.dart';
import '../models/booking.dart';
import '../theme/booking_theme.dart';

class BookingStatusTracker extends StatelessWidget {
  final BookingStatus status;
  final EscrowStatus? escrowStatus;
  final bool isCompact;

  const BookingStatusTracker({
    super.key,
    required this.status,
    this.escrowStatus,
    this.isCompact = false,
  });

  int get _currentStepIndex {
    switch (status) {
      case BookingStatus.pending:
        return 0;
      case BookingStatus.confirmed:
        return 1;
      case BookingStatus.completed:
        return 2;
      case BookingStatus.cancelled:
        return -1; // special handling
    }
  }

  @override
  Widget build(BuildContext context) {
    if (status == BookingStatus.cancelled) {
      return _buildCancelledBanner();
    }

    final currentIndex = _currentStepIndex;

    final steps = [
      _TrackerStep(
        title: 'Pending',
        subtitle: 'Booking requested',
        icon: Icons.hourglass_top_rounded,
      ),
      _TrackerStep(
        title: 'Confirmed',
        subtitle: escrowStatus == EscrowStatus.heldInEscrow
            ? 'Escrow secured'
            : 'Tour confirmed',
        icon: Icons.verified_user_rounded,
      ),
      _TrackerStep(
        title: 'Completed',
        subtitle: 'Tour finished & funds released',
        icon: Icons.task_alt_rounded,
      ),
    ];

    if (isCompact) {
      return Row(
        children: List.generate(steps.length * 2 - 1, (index) {
          if (index.isOdd) {
            final stepBefore = index ~/ 2;
            final isLinePassed = currentIndex > stepBefore;
            return Expanded(
              child: Container(
                height: 3,
                color: isLinePassed ? BookingTheme.primary : BookingTheme.border,
              ),
            );
          } else {
            final stepIndex = index ~/ 2;
            final isPassed = currentIndex >= stepIndex;
            return Container(
              width: 24,
              height: 24,
              decoration: BoxDecoration(
                shape: BoxShape.circle,
                color: isPassed ? BookingTheme.primary : Colors.white,
                border: Border.all(
                  color: isPassed ? BookingTheme.primary : BookingTheme.border,
                  width: 2,
                ),
              ),
              child: Icon(
                isPassed ? Icons.check : steps[stepIndex].icon,
                size: 14,
                color: isPassed ? Colors.white : BookingTheme.textMuted,
              ),
            );
          }
        }),
      );
    }

    return Container(
      padding: const EdgeInsets.all(20),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(20),
        border: Border.all(color: BookingTheme.border),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              const Text(
                'Booking Status',
                style: TextStyle(
                  fontSize: 16,
                  fontWeight: FontWeight.w800,
                  color: BookingTheme.forestDark,
                ),
              ),
              _buildStatusChip(status),
            ],
          ),
          const SizedBox(height: 22),
          Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: List.generate(steps.length * 2 - 1, (index) {
              if (index.isOdd) {
                final stepBefore = index ~/ 2;
                final isLinePassed = currentIndex > stepBefore;
                return Expanded(
                  child: Padding(
                    padding: const EdgeInsets.only(top: 18),
                    child: Container(
                      height: 3,
                      color: isLinePassed ? BookingTheme.primary : BookingTheme.border,
                    ),
                  ),
                );
              }

              final stepIdx = index ~/ 2;
              final isPassed = currentIndex >= stepIdx;
              final isCurrent = currentIndex == stepIdx;

              return SizedBox(
                width: 76,
                child: Column(
                  children: [
                    Container(
                      width: 38,
                      height: 38,
                      decoration: BoxDecoration(
                        shape: BoxShape.circle,
                        color: isPassed ? BookingTheme.primary : Colors.white,
                        border: Border.all(
                          color: isPassed
                              ? BookingTheme.primary
                              : BookingTheme.border,
                          width: 2,
                        ),
                        boxShadow: isCurrent
                            ? [
                                BoxShadow(
                                  color: BookingTheme.primary.withValues(alpha: 0.25),
                                  blurRadius: 8,
                                  offset: const Offset(0, 3),
                                ),
                              ]
                            : null,
                      ),
                      child: Icon(
                        isPassed && !isCurrent ? Icons.check_rounded : steps[stepIdx].icon,
                        size: 20,
                        color: isPassed ? Colors.white : BookingTheme.textMuted,
                      ),
                    ),
                    const SizedBox(height: 8),
                    Text(
                      steps[stepIdx].title,
                      textAlign: TextAlign.center,
                      style: TextStyle(
                        fontSize: 12,
                        fontWeight: isCurrent ? FontWeight.w800 : FontWeight.w600,
                        color: isCurrent
                            ? BookingTheme.primary
                            : (isPassed ? BookingTheme.forestDark : BookingTheme.textMuted),
                      ),
                    ),
                    const SizedBox(height: 2),
                    Text(
                      steps[stepIdx].subtitle,
                      textAlign: TextAlign.center,
                      maxLines: 2,
                      overflow: TextOverflow.ellipsis,
                      style: const TextStyle(
                        fontSize: 10,
                        color: BookingTheme.textMuted,
                        height: 1.1,
                      ),
                    ),
                  ],
                ),
              );
            }),
          ),
        ],
      ),
    );
  }

  Widget _buildCancelledBanner() {
    return Container(
      padding: const EdgeInsets.all(18),
      decoration: BoxDecoration(
        color: BookingTheme.errorRed.withValues(alpha: 0.08),
        borderRadius: BorderRadius.circular(20),
        border: Border.all(
          color: BookingTheme.errorRed.withValues(alpha: 0.3),
        ),
      ),
      child: Row(
        children: [
          Container(
            padding: const EdgeInsets.all(10),
            decoration: BoxDecoration(
              color: BookingTheme.errorRed.withValues(alpha: 0.15),
              shape: BoxShape.circle,
            ),
            child: const Icon(
              Icons.cancel_rounded,
              color: BookingTheme.errorRed,
              size: 26,
            ),
          ),
          const SizedBox(width: 14),
          const Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  'Booking Cancelled',
                  style: TextStyle(
                    fontWeight: FontWeight.w800,
                    fontSize: 15,
                    color: BookingTheme.errorRed,
                  ),
                ),
                SizedBox(height: 4),
                Text(
                  'This booking was cancelled. Any funds held in escrow have been refunded.',
                  style: TextStyle(
                    fontSize: 12,
                    color: BookingTheme.forestDark,
                  ),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildStatusChip(BookingStatus st) {
    Color bg;
    Color fg;
    switch (st) {
      case BookingStatus.pending:
        bg = BookingTheme.warningOrange.withValues(alpha: 0.12);
        fg = BookingTheme.warningOrange;
        break;
      case BookingStatus.confirmed:
        bg = BookingTheme.primary.withValues(alpha: 0.12);
        fg = BookingTheme.primary;
        break;
      case BookingStatus.completed:
        bg = BookingTheme.forestDark.withValues(alpha: 0.12);
        fg = BookingTheme.forestDark;
        break;
      case BookingStatus.cancelled:
        bg = BookingTheme.errorRed.withValues(alpha: 0.12);
        fg = BookingTheme.errorRed;
        break;
    }

    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
      decoration: BoxDecoration(
        color: bg,
        borderRadius: BorderRadius.circular(12),
      ),
      child: Text(
        st.label,
        style: TextStyle(
          fontSize: 12,
          fontWeight: FontWeight.w700,
          color: fg,
        ),
      ),
    );
  }
}

class _TrackerStep {
  final String title;
  final String subtitle;
  final IconData icon;

  const _TrackerStep({
    required this.title,
    required this.subtitle,
    required this.icon,
  });
}
