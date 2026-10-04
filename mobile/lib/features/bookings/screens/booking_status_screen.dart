import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/intl.dart';
import 'package:qr_flutter/qr_flutter.dart';

import '../models/booking.dart';
import '../providers/booking_providers.dart';
import '../services/booking_api_service.dart';
import '../theme/booking_theme.dart';
import '../widgets/booking_status_tracker.dart';
import '../widgets/error_state.dart';
import '../widgets/primary_gradient_button.dart';
import 'booking_history_screen.dart';
import 'discount_request_screen.dart';

class BookingStatusScreen extends ConsumerWidget {
  final String bookingId;

  const BookingStatusScreen({super.key, required this.bookingId});

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
                    content: Text(
                      'Booking successfully cancelled and escrow refunded.',
                    ),
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
    final compactWidth = MediaQuery.sizeOf(context).width < 360;
    final horizontalPadding = compactWidth ? 12.0 : 20.0;
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
        title: Text(
          'Booking #${booking.id}',
          maxLines: 1,
          overflow: TextOverflow.ellipsis,
        ),
        centerTitle: false,
        actions: [
          IconButton(
            icon: const Icon(Icons.refresh_rounded),
            tooltip: 'Refresh booking status',
            onPressed: () =>
                ref.read(travelerBookingsProvider.notifier).refresh(),
          ),
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
        padding: EdgeInsets.fromLTRB(
          horizontalPadding,
          16,
          horizontalPadding,
          40,
        ),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            // Status Tracker Stepper (Pending -> Confirmed -> Completed / Cancelled)
            BookingStatusTracker(
              status: booking.status,
              escrowStatus: booking.paymentStatus,
              isCompact: compactWidth,
            ),
            const SizedBox(height: 20),

            _buildTravelPass(booking),
            const SizedBox(height: 20),

            // Escrow Status Card
            Container(
              padding: EdgeInsets.all(compactWidth ? 12 : 18),
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
                          color: _escrowColor(booking.paymentStatus)
                              .withValues(alpha: 0.12),
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
                                  : (booking.paymentStatus == EscrowStatus.paid
                                    ? 'Payment received and confirmed by our team'
                                  : (booking.paymentStatus ==
                                            EscrowStatus.refunded
                                        ? 'Payment returned to traveler account'
                                    : 'Financial status tracked by Travyle escrow')),
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
                    _buildDetailRow('Transaction Ref', booking.transactionRef!),
                  ],
                ],
              ),
            ),
            const SizedBox(height: 20),
            // Tour Details Card
            Container(
              padding: EdgeInsets.all(compactWidth ? 12 : 18),
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
                    style: const TextStyle(
                      fontSize: 13,
                      color: BookingTheme.textMuted,
                    ),
                  ),
                  const Divider(height: 24, color: BookingTheme.border),
                  _buildDetailRow(
                    'Date',
                    DateFormat('EEEE, d MMMM y').format(booking.date),
                  ),
                  const SizedBox(height: 8),
                  _buildDetailRow('Time Slot', booking.timeSlot),
                  const SizedBox(height: 8),
                  _buildDetailRow('Travelers', '${booking.guests} Person(s)'),
                  const SizedBox(height: 8),
                  _buildDetailRow(
                    'Booked By',
                    '${booking.travelerName} (${booking.travelerEmail})',
                  ),
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
              padding: EdgeInsets.all(compactWidth ? 12 : 18),
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
                  _buildDetailRow(
                    'Base Trip',
                    currencyFormatter.format(booking.basePrice),
                  ),
                  const SizedBox(height: 8),
                  _buildDetailRow(
                    'Escrow & Platform Fee',
                    currencyFormatter.format(booking.serviceFee),
                  ),
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
                    children: [
                      const Expanded(
                        child: Text(
                          'Total Escrow Paid',
                          style: TextStyle(
                            fontSize: 16,
                            fontWeight: FontWeight.w800,
                            color: BookingTheme.forestDark,
                          ),
                        ),
                      ),
                      const SizedBox(width: 8),
                      Flexible(
                        child: Text(
                          currencyFormatter.format(booking.totalAmount),
                          textAlign: TextAlign.end,
                          maxLines: 1,
                          overflow: TextOverflow.ellipsis,
                          style: const TextStyle(
                            fontSize: 18,
                            fontWeight: FontWeight.w900,
                            color: BookingTheme.forestDark,
                          ),
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
                  side: BorderSide(
                    color: BookingTheme.errorRed.withValues(alpha: 0.5),
                  ),
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
              icon: const Icon(
                Icons.search_rounded,
                size: 18,
                color: Colors.white,
              ),
              text: 'Explore More Schedules',
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildTravelPass(Booking booking) {
    if (booking.status == BookingStatus.completed) {
      return Container(
        padding: const EdgeInsets.all(18),
        decoration: BoxDecoration(
          color: Colors.white,
          borderRadius: BorderRadius.circular(16),
          border: Border.all(color: BookingTheme.border),
        ),
        child: const Row(
          children: [
            Icon(Icons.check_circle, color: BookingTheme.primary, size: 28),
            SizedBox(width: 12),
            Expanded(
              child: Text(
                'Trip already finished',
                style: TextStyle(
                  fontSize: 16,
                  fontWeight: FontWeight.w800,
                  color: BookingTheme.forestDark,
                ),
              ),
            ),
          ],
        ),
      );
    }

    if (booking.status == BookingStatus.cancelled) {
      final cancelledPassUrl = Uri.parse(
        '$kBookingApiBaseUrl/api/bookings/${booking.id}/pass',
      ).toString();
      return Container(
        padding: const EdgeInsets.all(18),
        decoration: BoxDecoration(
          color: Colors.white,
          borderRadius: BorderRadius.circular(16),
          border: Border.all(color: BookingTheme.border),
        ),
        child: Column(
          children: [
            const Text(
              'Booking cancelled',
              style: TextStyle(
                fontSize: 17,
                fontWeight: FontWeight.w800,
                color: BookingTheme.errorRed,
              ),
            ),
            const SizedBox(height: 8),
            QrImageView(
              data: cancelledPassUrl,
              version: QrVersions.auto,
              size: 208,
              backgroundColor: Colors.white,
            ),
            const SizedBox(height: 8),
            const Text(
              'Scanning this code will show that this booking is cancelled.',
              textAlign: TextAlign.center,
              style: TextStyle(color: BookingTheme.textMuted),
            ),
          ],
        ),
      );
    }

    final isAgentBooking =
      booking.notes?.contains('Smart Booking Agent') ?? false;
    final paymentDue =
      isAgentBooking && booking.paymentStatus == EscrowStatus.pending;

    if (booking.status != BookingStatus.confirmed && !paymentDue) {
      return Container(
        padding: const EdgeInsets.all(16),
        decoration: BoxDecoration(
          color: Colors.white,
          borderRadius: BorderRadius.circular(16),
          border: Border.all(color: BookingTheme.border),
        ),
        child: Text(
          isAgentBooking
              ? 'AI booking: payment is due. Please contact our team to arrange payment. Your QR travel pass will be available after confirmation.'
              : 'Your QR travel pass will be available once this booking is confirmed.',
          style: const TextStyle(color: BookingTheme.textMuted),
        ),
      );
    }

    final revision = (booking.updatedAt ?? booking.createdAt)
        .toUtc()
        .toIso8601String();
    final passData = Uri.parse(
      '$kBookingApiBaseUrl/api/bookings/${booking.id}/pass',
    ).replace(queryParameters: {'revision': revision}).toString();

    return Container(
      padding: const EdgeInsets.all(18),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(16),
        border: Border.all(color: BookingTheme.border),
      ),
      child: Column(
        children: [
          Text(
            paymentDue ? 'AI booking pass · Payment due' : 'Confirmed travel pass',
            style: TextStyle(
              fontSize: 17,
              fontWeight: FontWeight.w800,
              color: paymentDue
                  ? BookingTheme.warningOrange
                  : BookingTheme.forestDark,
            ),
          ),
          const SizedBox(height: 4),
          Text(
            booking.id,
            style: const TextStyle(color: BookingTheme.textMuted),
          ),
          const SizedBox(height: 12),
          QrImageView(
            data: passData,
            version: QrVersions.auto,
            size: 208,
            backgroundColor: Colors.white,
          ),
          const SizedBox(height: 8),
          Text(
            paymentDue
                ? 'Payment is due. Scan to view passenger and trip details.'
                : 'Scan to view passenger and trip details',
            textAlign: TextAlign.center,
            style: const TextStyle(color: BookingTheme.textMuted),
          ),
        ],
      ),
    );
  }

  Color _escrowColor(EscrowStatus st) {
    switch (st) {
      case EscrowStatus.pending:
        return BookingTheme.warningOrange;
      case EscrowStatus.paid:
        return BookingTheme.primary;
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
      case EscrowStatus.paid:
        return Icons.check_circle_outline_rounded;
      case EscrowStatus.heldInEscrow:
        return Icons.lock_clock_rounded;
      case EscrowStatus.released:
        return Icons.check_circle_outline_rounded;
      case EscrowStatus.refunded:
        return Icons.replay_rounded;
    }
  }

  Widget _buildDetailRow(String label, String value, {Color? valueColor}) {
    return LayoutBuilder(
      builder: (context, constraints) {
        final labelText = Text(
          label,
          style: const TextStyle(fontSize: 13, color: BookingTheme.textMuted),
        );
        final valueText = Text(
          value,
          textAlign: TextAlign.end,
          maxLines: 3,
          overflow: TextOverflow.ellipsis,
          style: TextStyle(
            fontSize: 13,
            fontWeight: FontWeight.w600,
            color: valueColor ?? BookingTheme.forestDark,
          ),
        );

        if (constraints.maxWidth < 300) {
          return Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              labelText,
              Align(alignment: Alignment.centerRight, child: valueText),
            ],
          );
        }

        return Row(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Expanded(flex: 2, child: labelText),
            const SizedBox(width: 8),
            Expanded(flex: 3, child: valueText),
          ],
        );
      },
    );
  }
}
