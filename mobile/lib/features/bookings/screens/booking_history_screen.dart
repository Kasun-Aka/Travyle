import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/intl.dart';

import '../models/booking.dart';
import '../providers/booking_providers.dart';
import '../theme/booking_theme.dart';
import '../widgets/booking_status_tracker.dart';
import '../widgets/empty_state.dart';
import '../widgets/error_state.dart';
import '../widgets/loading_state.dart';
import 'booking_edit_screen.dart';
import 'booking_status_screen.dart';
import 'discount_request_history_screen.dart';
import 'schedule_browse_screen.dart';
import 'smart_booking_screen.dart';

class BookingHistoryScreen extends ConsumerStatefulWidget {
  const BookingHistoryScreen({super.key});

  @override
  ConsumerState<BookingHistoryScreen> createState() =>
      _BookingHistoryScreenState();
}

class _BookingHistoryScreenState extends ConsumerState<BookingHistoryScreen>
    with SingleTickerProviderStateMixin {
  late TabController _tabController;
  Timer? _bookingRefreshTimer;

  @override
  void initState() {
    super.initState();
    _tabController = TabController(length: 4, vsync: this);
    _bookingRefreshTimer = Timer.periodic(
      const Duration(seconds: 30),
      (_) => ref.read(travelerBookingsProvider.notifier).refresh(),
    );
    ref.read(travelerBookingsProvider.notifier).refresh();
  }

  @override
  void dispose() {
    _bookingRefreshTimer?.cancel();
    _tabController.dispose();
    super.dispose();
  }

  List<Booking> _filterBookings(List<Booking> bookings, int tabIndex) {
    switch (tabIndex) {
      case 1: // Active / Upcoming
        return bookings
            .where(
              (b) =>
                  b.status == BookingStatus.confirmed ||
                  b.status == BookingStatus.pending,
            )
            .toList();
      case 2: // Completed
        return bookings
            .where((b) => b.status == BookingStatus.completed)
            .toList();
      case 3: // Cancelled
        return bookings
            .where((b) => b.status == BookingStatus.cancelled)
            .toList();
      case 0: // All
      default:
        return bookings;
    }
  }

  @override
  Widget build(BuildContext context) {
    final bookingsAsync = ref.watch(travelerBookingsProvider);
    final currencyFormatter = NumberFormat.currency(
      symbol: 'LKR ',
      decimalDigits: 0,
    );

    return Scaffold(
      backgroundColor: BookingTheme.background,
      appBar: AppBar(
        title: const Text('My Saved Bookings'),
        actions: [
          IconButton(
            tooltip: 'Smart Booking Agent',
            icon: Container(
              padding: const EdgeInsets.all(8),
              decoration: BoxDecoration(
                color: Colors.white,
                shape: BoxShape.circle,
                border: Border.all(color: BookingTheme.border),
              ),
              child: const Icon(
                Icons.auto_awesome,
                color: BookingTheme.primary,
                size: 20,
              ),
            ),
            onPressed: () {
              Navigator.of(context).push(
                MaterialPageRoute(builder: (_) => const SmartBookingScreen()),
              );
            },
          ),
          IconButton(
            tooltip: 'Discount Requests',
            icon: Container(
              padding: const EdgeInsets.all(8),
              decoration: BoxDecoration(
                color: Colors.white,
                shape: BoxShape.circle,
                border: Border.all(color: BookingTheme.border),
              ),
              child: const Icon(
                Icons.percent_rounded,
                color: BookingTheme.primary,
                size: 20,
              ),
            ),
            onPressed: () {
              Navigator.of(context).push(
                MaterialPageRoute(
                  builder: (_) => const DiscountRequestHistoryScreen(),
                ),
              );
            },
          ),
          IconButton(
            tooltip: 'Browse Schedules',
            icon: Container(
              padding: const EdgeInsets.all(8),
              decoration: BoxDecoration(
                color: Colors.white,
                shape: BoxShape.circle,
                border: Border.all(color: BookingTheme.border),
              ),
              child: const Icon(
                Icons.explore_outlined,
                color: BookingTheme.primary,
                size: 20,
              ),
            ),
            onPressed: () {
              Navigator.of(context).push(
                MaterialPageRoute(
                  builder: (_) => const ScheduleBrowseScreen(),
                ),
              );
            },
          ),
          const SizedBox(width: 8),
        ],
        bottom: TabBar(
          controller: _tabController,
          labelColor: BookingTheme.primary,
          unselectedLabelColor: BookingTheme.textMuted,
          indicatorColor: BookingTheme.primary,
          indicatorWeight: 3,
          labelStyle: const TextStyle(
            fontWeight: FontWeight.w800,
            fontSize: 13,
          ),
          tabs: const [
            Tab(text: 'All'),
            Tab(text: 'Active'),
            Tab(text: 'Completed'),
            Tab(text: 'Cancelled'),
          ],
        ),
      ),
      floatingActionButton: FloatingActionButton.extended(
        backgroundColor: BookingTheme.primary,
        foregroundColor: Colors.white,
        icon: const Icon(Icons.auto_awesome),
        label: const Text(
          'Smart Booking Agent',
          style: TextStyle(fontWeight: FontWeight.w800),
        ),
        onPressed: () {
          Navigator.of(context).push(
            MaterialPageRoute(builder: (_) => const SmartBookingScreen()),
          );
        },
      ),
      body: Column(
        children: [
          Container(
            margin: const EdgeInsets.fromLTRB(16, 12, 16, 4),
            padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 8),
            decoration: BoxDecoration(
              color: Colors.white,
              borderRadius: BorderRadius.circular(16),
              border: Border.all(color: BookingTheme.border),
            ),
            child: Row(
              children: [
                Expanded(
                  child: InkWell(
                    onTap: () => Navigator.of(context).push(
                      MaterialPageRoute(
                        builder: (_) => const ScheduleBrowseScreen(),
                      ),
                    ),
                    borderRadius: BorderRadius.circular(10),
                    child: const Padding(
                      padding: EdgeInsets.symmetric(vertical: 6),
                      child: Row(
                        mainAxisAlignment: MainAxisAlignment.center,
                        children: [
                          Icon(
                            Icons.explore_rounded,
                            size: 18,
                            color: BookingTheme.primary,
                          ),
                          SizedBox(width: 6),
                          Text(
                            'Browse Tours',
                            style: TextStyle(
                              fontSize: 12,
                              fontWeight: FontWeight.w700,
                              color: BookingTheme.forestDark,
                            ),
                          ),
                        ],
                      ),
                    ),
                  ),
                ),
                Container(width: 1, height: 24, color: BookingTheme.border),
                Expanded(
                  child: InkWell(
                    onTap: () => Navigator.of(context).push(
                      MaterialPageRoute(
                        builder: (_) => const SmartBookingScreen(),
                      ),
                    ),
                    borderRadius: BorderRadius.circular(10),
                    child: const Padding(
                      padding: EdgeInsets.symmetric(vertical: 6),
                      child: Row(
                        mainAxisAlignment: MainAxisAlignment.center,
                        children: [
                          Icon(
                            Icons.auto_awesome,
                            size: 18,
                            color: BookingTheme.primary,
                          ),
                          SizedBox(width: 6),
                          Text(
                            'AI Agent',
                            style: TextStyle(
                              fontSize: 12,
                              fontWeight: FontWeight.w700,
                              color: BookingTheme.forestDark,
                            ),
                          ),
                        ],
                      ),
                    ),
                  ),
                ),
                Container(width: 1, height: 24, color: BookingTheme.border),
                Expanded(
                  child: InkWell(
                    onTap: () => Navigator.of(context).push(
                      MaterialPageRoute(
                        builder: (_) => const DiscountRequestHistoryScreen(),
                      ),
                    ),
                    borderRadius: BorderRadius.circular(10),
                    child: const Padding(
                      padding: EdgeInsets.symmetric(vertical: 6),
                      child: Row(
                        mainAxisAlignment: MainAxisAlignment.center,
                        children: [
                          Icon(
                            Icons.percent_rounded,
                            size: 18,
                            color: BookingTheme.primary,
                          ),
                          SizedBox(width: 6),
                          Text(
                            'Discounts',
                            style: TextStyle(
                              fontSize: 12,
                              fontWeight: FontWeight.w700,
                              color: BookingTheme.forestDark,
                            ),
                          ),
                        ],
                      ),
                    ),
                  ),
                ),
              ],
            ),
          ),
          Expanded(
            child: bookingsAsync.when(
              loading: () =>
                  const LoadingState(message: 'Loading your bookings...'),
              error: (err, _) => ErrorState(
                message: err.toString(),
                onRetry: () =>
                    ref.read(travelerBookingsProvider.notifier).refresh(),
              ),
              data: (allBookings) {
                return TabBarView(
                  controller: _tabController,
                  children: List.generate(4, (tabIdx) {
                    final filtered = _filterBookings(allBookings, tabIdx);

                    if (filtered.isEmpty) {
                      return EmptyState(
                        title: 'No bookings found',
                        description: tabIdx == 0
                            ? 'You have not booked any travel schedules yet.'
                            : 'No bookings in this category.',
                        icon: Icons.confirmation_number_outlined,
                        actionText: 'Browse Tours',
                        onAction: () => Navigator.of(context).push(
                          MaterialPageRoute(
                            builder: (_) => const ScheduleBrowseScreen(),
                          ),
                        ),
                      );
                    }

                    return RefreshIndicator(
                      color: BookingTheme.primary,
                      onRefresh: () async {
                        ref.read(travelerBookingsProvider.notifier).refresh();
                      },
                      child: ListView.separated(
                        padding: const EdgeInsets.all(20),
                        itemCount: filtered.length,
                        separatorBuilder: (_, _) => const SizedBox(height: 16),
                        itemBuilder: (context, index) {
                          final booking = filtered[index];
                          return _buildBookingCard(
                            context,
                            booking,
                            currencyFormatter,
                          );
                        },
                      ),
                    );
                  }),
                );
              },
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildBookingCard(
    BuildContext context,
    Booking booking,
    NumberFormat currencyFormatter,
  ) {
    final today = DateTime.now();
    final todayOnly = DateTime(today.year, today.month, today.day);
    final bookingDay = DateTime(
      booking.date.year,
      booking.date.month,
      booking.date.day,
    );
    final isUpcoming =
        bookingDay.isAfter(todayOnly) &&
        (booking.status == BookingStatus.pending ||
            booking.status == BookingStatus.confirmed);
    final canDelete =
        bookingDay.isBefore(todayOnly) ||
        booking.status == BookingStatus.completed;

    return InkWell(
      onTap: () {
        Navigator.of(context).push(
          MaterialPageRoute(
            builder: (_) => BookingStatusScreen(bookingId: booking.id),
          ),
        );
      },
      borderRadius: BorderRadius.circular(20),
      child: Container(
        padding: const EdgeInsets.all(18),
        decoration: BoxDecoration(
          color: Colors.white,
          borderRadius: BorderRadius.circular(20),
          border: Border.all(color: BookingTheme.border),
          boxShadow: [
            BoxShadow(
              color: Colors.black.withValues(alpha: 0.03),
              blurRadius: 10,
              offset: const Offset(0, 4),
            ),
          ],
        ),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // Reference & Status Badge
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                Text(
                  '#${booking.id}',
                  style: const TextStyle(
                    fontSize: 13,
                    fontWeight: FontWeight.w700,
                    color: BookingTheme.textMuted,
                  ),
                ),
                _buildStatusChip(booking.status),
              ],
            ),
            const SizedBox(height: 10),

            // Tour Title & Location
            Text(
              booking.destinationTitle,
              style: const TextStyle(
                fontSize: 16,
                fontWeight: FontWeight.w800,
                color: BookingTheme.forestDark,
              ),
            ),
            const SizedBox(height: 4),
            Text(
              booking.location,
              style: const TextStyle(
                fontSize: 12,
                color: BookingTheme.textMuted,
              ),
            ),
            const SizedBox(height: 14),

            // Compact Stepper Tracker
            BookingStatusTracker(
              status: booking.status,
              escrowStatus: booking.paymentStatus,
              isCompact: true,
            ),
            const SizedBox(height: 14),

            const Divider(height: 1, color: BookingTheme.border),
            const SizedBox(height: 12),

            // Date, Guests, and Total Price
            Row(
              children: [
                Icon(
                  Icons.calendar_today_rounded,
                  size: 14,
                  color: BookingTheme.primary,
                ),
                const SizedBox(width: 6),
                Text(
                  DateFormat('EEE, d MMM').format(booking.date),
                  style: const TextStyle(
                    fontSize: 12,
                    fontWeight: FontWeight.w600,
                    color: BookingTheme.forestDark,
                  ),
                ),
                const SizedBox(width: 12),
                Icon(
                  Icons.access_time_rounded,
                  size: 14,
                  color: BookingTheme.primary,
                ),
                const SizedBox(width: 4),
                Text(
                  booking.timeSlot,
                  style: const TextStyle(
                    fontSize: 12,
                    color: BookingTheme.forestDark,
                  ),
                ),
                const Spacer(),
                Text(
                  currencyFormatter.format(booking.totalAmount),
                  style: const TextStyle(
                    fontSize: 16,
                    fontWeight: FontWeight.w900,
                    color: BookingTheme.forestDark,
                  ),
                ),
              ],
            ),
            if (isUpcoming || canDelete) ...[
              const SizedBox(height: 12),
              const Divider(height: 1, color: BookingTheme.border),
              const SizedBox(height: 8),
              Row(
                mainAxisAlignment: MainAxisAlignment.end,
                children: [
                  if (isUpcoming)
                    TextButton.icon(
                      onPressed: () async {
                        await Navigator.of(context).push<bool>(
                          MaterialPageRoute(
                            builder: (_) => BookingEditScreen(booking: booking),
                          ),
                        );
                      },
                      icon: const Icon(Icons.edit_outlined, size: 18),
                      label: const Text('Edit'),
                    ),
                  if (canDelete)
                    TextButton.icon(
                      onPressed: () =>
                          _deleteHistoricalBooking(context, booking),
                      style: TextButton.styleFrom(
                        foregroundColor: BookingTheme.errorRed,
                      ),
                      icon: const Icon(Icons.delete_outline, size: 18),
                      label: const Text('Delete'),
                    ),
                ],
              ),
            ],
          ],
        ),
      ),
    );
  }

  Future<void> _deleteHistoricalBooking(
    BuildContext context,
    Booking booking,
  ) async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (dialogContext) => AlertDialog(
        title: const Text('Delete saved booking?'),
        content: const Text(
          'This booking is in the past and will be removed from your saved bookings.',
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(dialogContext).pop(false),
            child: const Text('Keep'),
          ),
          FilledButton(
            onPressed: () => Navigator.of(dialogContext).pop(true),
            style: FilledButton.styleFrom(
              backgroundColor: BookingTheme.errorRed,
            ),
            child: const Text('Delete'),
          ),
        ],
      ),
    );
    if (confirmed != true || !context.mounted) return;

    final deleted = await ref
        .read(cancelBookingProvider.notifier)
        .cancel(booking.id);
    if (!context.mounted) return;

    if (deleted) {
      ref.read(travelerBookingsProvider.notifier).removeBooking(booking.id);
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Booking deleted from saved bookings.')),
      );
    } else {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Unable to delete this booking.')),
      );
    }
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
        borderRadius: BorderRadius.circular(10),
      ),
      child: Text(
        st.label,
        style: TextStyle(fontSize: 11, fontWeight: FontWeight.w700, color: fg),
      ),
    );
  }
}
