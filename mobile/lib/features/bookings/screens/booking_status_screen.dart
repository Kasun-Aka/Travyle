import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/intl.dart';
import '../models/booking.dart';
import '../providers/booking_providers.dart';
import '../theme/booking_theme.dart';
import '../widgets/booking_status_tracker.dart';
import '../widgets/error_state.dart';
import '../widgets/primary_gradient_button.dart';
import 'booking_history_screen.dart';
import 'discount_request_screen.dart';

class BookingStatusScreen extends ConsumerWidget {
  final String bookingId;

  const BookingStatusScreen({
    super.key,
    required this.bookingId,
  });

  void _showCancelDialog(BuildContext context, WidgetRef ref, Booking booking) {
    showDialog(
      context: context,
      builder: (dialogCtx) => AlertDialog(
        title: const Text('Cancel Booking?'),
        content: Text(
          'Are you sure you want to cancel booking #${booking.id}? '
          'Since funds are held in escrow, your payment will be refunded.',
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(dialogCtx).pop(),
            child: const Text('Keep Booking'),
          ),
          ElevatedButton(
            onPressed: () async {
              Navigator.of(dialogCtx).pop();
              final success = await ref
                  .read(cancelBookingProvider.notifier)
                  .cancel(booking.id);
              if (context.mounted && success) {
                ScaffoldMessenger.of(context).showSnackBar(
                  const SnackBar(
                    content: Text('Booking successfully cancelled and escrow refunded.'),
                    backgroundColor: BookingTheme.errorRed,
                  ),
                );
              }
            },
            style: ElevatedButton.styleFrom(
              backgroundColor: BookingTheme.errorRed,
              foregroundColor: Colors.white,
            ),
            child: const Text('Confirm Cancellation'),
          ),
        ],
      ),
    );
  }

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final booking = ref.watch(bookingDetailProvider(bookingId));
    final currencyFormatter = NumberFormat.currency(
      symbol: 'LKR ',
      decimalDigits: 2,
    );

    if (booking == null) {
      return Scaffold(
        appBar: AppBar(title: const Text('Booking Details')),
        body: ErrorState(
          title: 'Booking Not Found',
          message: 'Could not find booking reference #$bookingId',
          onRetry: () => Navigator.of(context).pop(),
        ),
      );
    }

    return Scaffold(
      backgroundColor: BookingTheme.background,
      appBar: AppBar(
        title: Text('Booking #${booking.id}'),
        centerTitle: false,
        actions: [
          IconButton(
            icon: const Icon(Icons.history_rounded),
            tooltip: 'My Bookings',
            onPressed: () {
              Navigator.of(context).push(
                MaterialPageRoute(builder: (_) => const BookingHistoryScreen()),
              );
            },
          ),
        ],
      ),
      body: SingleChildScrollView(
        padding: const EdgeInsets.fromLTRB(20, 16, 20, 40),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // Status Tracker Stepper (Pending -> Confirmed -> Completed / Cancelled)
            BookingStatusTracker(
              status: booking.status,
              escrowStatus: booking.paymentStatus,
            ),
            const SizedBox(height: 20),

            // Escrow Status Card
            Container(
              padding: const EdgeInsets.all(18),
              decoration: BoxDecoration(
                color: Colors.white,
                borderRadius: BorderRadius.circular(20),
                border: Border.all(color: BookingTheme.border),
              ),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Row(
                    children: [
                      Container(
                        padding: const EdgeInsets.all(10),
                        decoration: BoxDecoration(
                          color: _escrowColor(booking.paymentStatus).withValues(alpha: 0.12),
                          shape: BoxShape.circle,
                        ),
                        child: Icon(
                          _escrowIcon(booking.paymentStatus),
                          color: _escrowColor(booking.paymentStatus),
                          size: 22,
                        ),
                      ),
                      const SizedBox(width: 14),
                      Expanded(
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Text(
                              booking.paymentStatus.label,
                              style: TextStyle(
                                fontSize: 16,
                                fontWeight: FontWeight.w800,
                                color: _escrowColor(booking.paymentStatus),
                              ),
                            ),
                            const SizedBox(height: 2),
                            Text(
                              booking.paymentStatus == EscrowStatus.heldInEscrow
                                  ? 'Funds locked in Stripe Escrow until tour completes'
                                  : (booking.paymentStatus == EscrowStatus.refunded
                                      ? 'Payment returned to traveler account'
                                      : 'Financial status tracked by Travyle escrow'),
                              style: const TextStyle(
                                fontSize: 12,
                                color: BookingTheme.textMuted,
                              ),
                            ),
                          ],
                        ),
                      ),
                    ],
                  ),
                  if (booking.transactionRef != null) ...[
                    const Divider(height: 24, color: BookingTheme.border),
                    Row(
                      mainAxisAlignment: MainAxisAlignment.spaceBetween,
                      children: [
                        const Text(
                          'Transaction Ref',
                          style: TextStyle(fontSize: 12, color: BookingTheme.textMuted),
                        ),
                        Text(
                          booking.transactionRef!,
                          style: const TextStyle(
                            fontSize: 12,
                            fontWeight: FontWeight.w600,
                            color: BookingTheme.forestDark,
                          ),
                        ),
                      ],
                    ),
                  ],
                ],
              ),
            ),
            const SizedBox(height: 20),

            // Tour Details Card
            Container(
              padding: const EdgeInsets.all(18),
              decoration: BoxDecoration(
                color: Colors.white,
                borderRadius: BorderRadius.circular(20),
                border: Border.all(color: BookingTheme.border),
              ),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  const Text(
                    'Tour Details',
                    style: TextStyle(
                      fontSize: 16,
                      fontWeight: FontWeight.w800,
                      color: BookingTheme.forestDark,
                    ),
                  ),
                  const SizedBox(height: 12),
                  Text(
                    booking.destinationTitle,
                    style: const TextStyle(
                      fontSize: 18,
                      fontWeight: FontWeight.w800,
                      color: BookingTheme.forestDark,
                    ),
                  ),
                  const SizedBox(height: 4),
                  Text(
                    booking.location,
                    style: const TextStyle(fontSize: 13, color: BookingTheme.textMuted),
                  ),
                  const Divider(height: 24, color: BookingTheme.border),
                  _buildDetailRow('Date', DateFormat('EEEE, d MMMM y').format(booking.date)),
                  const SizedBox(height: 8),
                  _buildDetailRow('Time Slot', booking.timeSlot),
                  const SizedBox(height: 8),
                  _buildDetailRow('Travelers', '${booking.guests} Person(s)'),
                  const SizedBox(height: 8),
                  _buildDetailRow('Booked By', '${booking.travelerName} (${booking.travelerEmail})'),
                  if (booking.notes != null && booking.notes!.isNotEmpty) ...[
                    const SizedBox(height: 8),
                    _buildDetailRow('Special Notes', booking.notes!),
                  ],
                ],
              ),
            ),
            const SizedBox(height: 20),

            // Payment Summary Card
            Container(
              padding: const EdgeInsets.all(18),
              decoration: BoxDecoration(
                color: Colors.white,
                borderRadius: BorderRadius.circular(20),
                border: Border.all(color: BookingTheme.border),
              ),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  const Text(
                    'Payment Summary',
                    style: TextStyle(
                      fontSize: 16,
                      fontWeight: FontWeight.w800,
                      color: BookingTheme.forestDark,
                    ),
                  ),
                  const SizedBox(height: 12),
                  _buildDetailRow('Base Trip', currencyFormatter.format(booking.basePrice)),
                  const SizedBox(height: 8),
                  _buildDetailRow('Escrow & Platform Fee', currencyFormatter.format(booking.serviceFee)),
                  if (booking.discountAmount > 0) ...[
                    const SizedBox(height: 8),
                    _buildDetailRow(
                      'Discount Applied',
                      '- ${currencyFormatter.format(booking.discountAmount)}',
                      valueColor: BookingTheme.primary,
                    ),
                  ],
                  const Divider(height: 24, color: BookingTheme.border),
                  Row(
                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                    children: [
                      const Text(
                        'Total Escrow Paid',
                        style: TextStyle(
                          fontSize: 16,
                          fontWeight: FontWeight.w800,
                          color: BookingTheme.forestDark,
                        ),
                      ),
                      Text(
                        currencyFormatter.format(booking.totalAmount),
                        style: const TextStyle(
                          fontSize: 18,
                          fontWeight: FontWeight.w900,
                          color: BookingTheme.forestDark,
                        ),
                      ),
                    ],
                  ),
                ],
              ),
            ),
            const SizedBox(height: 24),

            // Action Buttons
            if (booking.status != BookingStatus.cancelled &&
                booking.status != BookingStatus.completed) ...[
              // Request Discount Button
              OutlinedButton.icon(
                onPressed: () {
                  Navigator.of(context).push(
                    MaterialPageRoute(
                      builder: (_) => DiscountRequestScreen(booking: booking),
                    ),
                  );
                },
                icon: const Icon(Icons.percent_rounded, size: 18),
                label: const Text('Request Special Concession / Discount'),
                style: OutlinedButton.styleFrom(
                  foregroundColor: BookingTheme.forestDark,
                  side: const BorderSide(color: BookingTheme.border),
                  padding: const EdgeInsets.symmetric(vertical: 14),
                  minimumSize: const Size.fromHeight(50),
                  shape: RoundedRectangleBorder(
                    borderRadius: BorderRadius.circular(14),
                  ),
                ),
              ),
              const SizedBox(height: 12),

              // Cancel Booking Button
              OutlinedButton.icon(
                onPressed: () => _showCancelDialog(context, ref, booking),
                icon: const Icon(Icons.cancel_outlined, size: 18),
                label: const Text('Cancel Booking & Refund Escrow'),
                style: OutlinedButton.styleFrom(
                  foregroundColor: BookingTheme.errorRed,
                  side: BorderSide(color: BookingTheme.errorRed.withValues(alpha: 0.5)),
                  padding: const EdgeInsets.symmetric(vertical: 14),
                  minimumSize: const Size.fromHeight(50),
                  shape: RoundedRectangleBorder(
                    borderRadius: BorderRadius.circular(14),
                  ),
                ),
              ),
              const SizedBox(height: 12),
            ],

            // Return to Browse Schedules
            PrimaryGradientButton(
              width: double.infinity,
              height: 50,
              onPressed: () {
                Navigator.of(context).pushReplacement(
                  MaterialPageRoute(
                    builder: (_) => const BookingHistoryScreen(),
                  ),
                );
              },
              icon: const Icon(Icons.search_rounded, size: 18, color: Colors.white),
              text: 'Explore More Schedules',
            ),
          ],
        ),
      ),
    );
  }

  Color _escrowColor(EscrowStatus st) {
    switch (st) {
      case EscrowStatus.pending:
        return BookingTheme.warningOrange;
      case EscrowStatus.heldInEscrow:
        return BookingTheme.primary;
      case EscrowStatus.released:
        return BookingTheme.forestDark;
      case EscrowStatus.refunded:
        return BookingTheme.errorRed;
    }
  }

  IconData _escrowIcon(EscrowStatus st) {
    switch (st) {
      case EscrowStatus.pending:
        return Icons.hourglass_top_rounded;
      case EscrowStatus.heldInEscrow:
        return Icons.lock_clock_rounded;
      case EscrowStatus.released:
        return Icons.check_circle_outline_rounded;
      case EscrowStatus.refunded:
        return Icons.replay_rounded;
    }
  }

  Widget _buildDetailRow(String label, String value, {Color? valueColor}) {
    return Row(
      crossAxisAlignment: CrossAxisAlignment.start,
      mainAxisAlignment: MainAxisAlignment.spaceBetween,
      children: [
        Text(
          label,
          style: const TextStyle(fontSize: 13, color: BookingTheme.textMuted),
        ),
        const SizedBox(width: 16),
        Flexible(
          child: Text(
            value,
            textAlign: TextAlign.end,
            style: TextStyle(
              fontSize: 13,
              fontWeight: FontWeight.w600,
              color: valueColor ?? BookingTheme.forestDark,
            ),
          ),
        ),
      ],
    );
  }
}
