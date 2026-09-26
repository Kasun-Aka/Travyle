import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/intl.dart';

import '../models/booking.dart';
import '../providers/booking_providers.dart';
import '../theme/booking_theme.dart';
import '../widgets/error_state.dart';
import '../widgets/loading_state.dart';

class DiscountRequestHistoryScreen extends ConsumerWidget {
  const DiscountRequestHistoryScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final requestsAsync = ref.watch(discountRequestsProvider);

    return Scaffold(
      backgroundColor: BookingTheme.background,
      appBar: AppBar(title: const Text('Discount Requests')),
      body: requestsAsync.when(
        loading: () =>
            const LoadingState(message: 'Loading discount requests...'),
        error: (error, _) => ErrorState(
          message: error.toString(),
          onRetry: () => ref.read(discountRequestsProvider.notifier).refresh(),
        ),
        data: (requests) {
          if (requests.isEmpty) {
            return RefreshIndicator(
              color: BookingTheme.primary,
              onRefresh: () =>
                  ref.read(discountRequestsProvider.notifier).refresh(),
              child: ListView(
                padding: const EdgeInsets.all(24),
                children: const [
                  SizedBox(height: 80),
                  Icon(
                    Icons.percent_outlined,
                    size: 54,
                    color: BookingTheme.primary,
                  ),
                  SizedBox(height: 18),
                  Text(
                    'No discount requests yet',
                    textAlign: TextAlign.center,
                    style: TextStyle(
                      fontSize: 20,
                      fontWeight: FontWeight.w800,
                      color: BookingTheme.forestDark,
                    ),
                  ),
                  SizedBox(height: 8),
                  Text(
                    'Requests you submit after booking will appear here with their latest status.',
                    textAlign: TextAlign.center,
                    style: TextStyle(
                      color: BookingTheme.textMuted,
                      height: 1.4,
                    ),
                  ),
                ],
              ),
            );
          }

          return LayoutBuilder(
            builder: (context, constraints) {
              final horizontalPadding = constraints.maxWidth < 360
                  ? 14.0
                  : 20.0;
              return RefreshIndicator(
                color: BookingTheme.primary,
                onRefresh: () =>
                    ref.read(discountRequestsProvider.notifier).refresh(),
                child: ListView.separated(
                  padding: EdgeInsets.symmetric(
                    horizontal: horizontalPadding,
                    vertical: 20,
                  ),
                  itemCount: requests.length,
                  separatorBuilder: (_, _) => const SizedBox(height: 14),
                  itemBuilder: (context, index) =>
                      _RequestCard(request: requests[index]),
                ),
              );
            },
          );
        },
      ),
    );
  }
}

class _RequestCard extends StatelessWidget {
  final DiscountRequest request;

  const _RequestCard({required this.request});

  @override
  Widget build(BuildContext context) {
    final statusColor = switch (request.status) {
      DiscountStatus.pending => const Color(0xFFF59E0B),
      DiscountStatus.approved => BookingTheme.primary,
      DiscountStatus.rejected => BookingTheme.errorRed,
    };
    final currency = NumberFormat.currency(symbol: 'LKR ', decimalDigits: 0);

    return Container(
      padding: const EdgeInsets.all(18),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(18),
        border: Border.all(color: BookingTheme.border),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          LayoutBuilder(
            builder: (context, constraints) {
              final status = Container(
                padding: const EdgeInsets.symmetric(
                  horizontal: 10,
                  vertical: 6,
                ),
                decoration: BoxDecoration(
                  color: statusColor.withValues(alpha: 0.1),
                  borderRadius: BorderRadius.circular(20),
                ),
                child: Text(
                  request.status.userLabel,
                  style: TextStyle(
                    fontSize: 12,
                    fontWeight: FontWeight.w800,
                    color: statusColor,
                  ),
                ),
              );
              final booking = Text(
                'Booking #${request.bookingId}',
                maxLines: 2,
                overflow: TextOverflow.ellipsis,
                style: const TextStyle(
                  fontWeight: FontWeight.w800,
                  color: BookingTheme.forestDark,
                ),
              );

              if (constraints.maxWidth < 320) {
                return Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [booking, const SizedBox(height: 8), status],
                );
              }

              return Row(
                children: [
                  Expanded(child: booking),
                  const SizedBox(width: 8),
                  status,
                ],
              );
            },
          ),
          const SizedBox(height: 14),
          Text(
            '${request.requestedDiscountPercent.toStringAsFixed(0)}% discount request',
            style: const TextStyle(
              fontSize: 16,
              fontWeight: FontWeight.w700,
              color: BookingTheme.forestDark,
            ),
          ),
          const SizedBox(height: 4),
          Text(
            'Amount: ${currency.format(request.calculatedDiscountAmount)}',
            style: const TextStyle(color: BookingTheme.textMuted),
          ),
          const SizedBox(height: 12),
          Text(
            request.status == DiscountStatus.pending
                ? 'Your proof is waiting for administrator review.'
                : request.status == DiscountStatus.approved
                ? 'The approved amount has been refunded to your payment.'
                : 'This request was cancelled and the original booking amount remains unchanged.',
            style: const TextStyle(
              fontSize: 12,
              color: BookingTheme.textMuted,
              height: 1.35,
            ),
          ),
          const SizedBox(height: 10),
          Text(
            DateFormat('d MMM yyyy, h:mm a').format(request.createdAt),
            style: const TextStyle(fontSize: 11, color: BookingTheme.textMuted),
          ),
        ],
      ),
    );
  }
}
