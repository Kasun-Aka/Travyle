import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/intl.dart';

import '../models/booking.dart';
import '../providers/booking_providers.dart';
import '../theme/booking_theme.dart';
import '../widgets/empty_state.dart';
import '../widgets/error_state.dart';
import '../widgets/loading_state.dart';
import '../widgets/schedule_card.dart';
import 'booking_history_screen.dart';
import 'discount_request_history_screen.dart';
import 'slot_selection_screen.dart';

class ScheduleBrowseScreen extends ConsumerStatefulWidget {
  const ScheduleBrowseScreen({super.key});

  @override
  ConsumerState<ScheduleBrowseScreen> createState() =>
      _ScheduleBrowseScreenState();
}

class _ScheduleBrowseScreenState extends ConsumerState<ScheduleBrowseScreen> {
  final TextEditingController _searchController = TextEditingController();

  final List<String> _destinationPresets = const [
    'All',
    'Ella',
    'Sigiriya',
    'Mirissa',
  ];

  @override
  void initState() {
    super.initState();
    ref.read(scheduleListProvider.notifier).loadSchedules();
  }

  @override
  void dispose() {
    _searchController.dispose();
    super.dispose();
  }

  Future<void> _pickDateFilter() async {
    final now = DateTime.now();
    final current = ref.read(scheduleFilterProvider).filterDate ?? now;
    final picked = await showDatePicker(
      context: context,
      initialDate: current,
      firstDate: now,
      lastDate: now.add(const Duration(days: 60)),
      builder: (context, child) {
        return Theme(
          data: Theme.of(context).copyWith(
            colorScheme: const ColorScheme.light(
              primary: BookingTheme.primary,
              onPrimary: Colors.white,
              surface: Colors.white,
              onSurface: BookingTheme.forestDark,
            ),
          ),
          child: child!,
        );
      },
    );

    if (picked != null) {
      ref
          .read(scheduleFilterProvider.notifier)
          .update((state) => state.copyWith(filterDate: picked));
    }
  }

  void _clearDateFilter() {
    ref
        .read(scheduleFilterProvider.notifier)
        .update((state) => state.copyWith(clearFilterDate: true));
  }

  @override
  Widget build(BuildContext context) {
    final filterState = ref.watch(scheduleFilterProvider);
    final schedulesAsync = ref.watch(filteredSchedulesProvider);

    return Scaffold(
      backgroundColor: BookingTheme.background,
      appBar: AppBar(
        title: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Text(
              'Explore Schedules',
              style: TextStyle(
                fontSize: 22,
                fontWeight: FontWeight.w900,
                color: BookingTheme.forestDark,
              ),
            ),
            Text(
              'Book certified local tours & treks',
              style: TextStyle(
                fontSize: 12,
                color: BookingTheme.forestDark.withValues(alpha: 0.6),
                fontWeight: FontWeight.w500,
              ),
            ),
          ],
        ),
        actions: [
          IconButton(
            tooltip: 'My Bookings',
            icon: Container(
              padding: const EdgeInsets.all(8),
              decoration: BoxDecoration(
                color: Colors.white,
                shape: BoxShape.circle,
                border: Border.all(color: BookingTheme.border),
              ),
              child: const Icon(
                Icons.confirmation_number_outlined,
                color: BookingTheme.primary,
                size: 20,
              ),
            ),
            onPressed: () {
              Navigator.of(context).push(
                MaterialPageRoute(builder: (_) => const BookingHistoryScreen()),
              );
            },
          ),
          const SizedBox(width: 8),
        ],
      ),
      body: RefreshIndicator(
        color: BookingTheme.primary,
        onRefresh: () async {
          ref.read(scheduleListProvider.notifier).loadSchedules();
        },
        child: CustomScrollView(
          slivers: [
            SliverToBoxAdapter(
              child: Padding(
                padding: const EdgeInsets.fromLTRB(20, 16, 20, 4),
                child: Container(
                  padding: const EdgeInsets.all(6),
                  decoration: BoxDecoration(
                    color: Colors.white,
                    borderRadius: BorderRadius.circular(18),
                    border: Border.all(color: BookingTheme.border),
                  ),
                  child: Column(
                    children: [
                      ListTile(
                        leading: const Icon(
                          Icons.confirmation_number_rounded,
                          color: BookingTheme.primary,
                        ),
                        title: const Text(
                          'My saved bookings',
                          style: TextStyle(fontWeight: FontWeight.w800),
                        ),
                        subtitle: const Text(
                          'View trips, payment and booking status',
                        ),
                        trailing: const Icon(Icons.chevron_right_rounded),
                        onTap: () => Navigator.of(context).push(
                          MaterialPageRoute(
                            builder: (_) => const BookingHistoryScreen(),
                          ),
                        ),
                      ),
                      const Divider(height: 1, color: BookingTheme.border),
                      ListTile(
                        leading: const Icon(
                          Icons.percent_rounded,
                          color: BookingTheme.primary,
                        ),
                        title: const Text(
                          'Discount request status',
                          style: TextStyle(fontWeight: FontWeight.w800),
                        ),
                        subtitle: const Text(
                          'Track review, refund or cancellation',
                        ),
                        trailing: const Icon(Icons.chevron_right_rounded),
                        onTap: () => Navigator.of(context).push(
                          MaterialPageRoute(
                            builder: (_) =>
                                const DiscountRequestHistoryScreen(),
                          ),
                        ),
                      ),
                    ],
                  ),
                ),
              ),
            ),
            // Search & Filters Header
            SliverToBoxAdapter(
              child: Padding(
                padding: const EdgeInsets.fromLTRB(20, 16, 20, 8),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    // Search field
                    TextField(
                      controller: _searchController,
                      onChanged: (val) {
                        ref
                            .read(scheduleFilterProvider.notifier)
                            .update(
                              (state) => state.copyWith(searchQuery: val),
                            );
                      },
                      decoration: InputDecoration(
                        hintText: 'Search tours, locations, guides...',
                        hintStyle: const TextStyle(
                          color: BookingTheme.textMuted,
                          fontSize: 14,
                        ),
                        prefixIcon: const Icon(
                          Icons.search_rounded,
                          color: BookingTheme.primary,
                        ),
                        suffixIcon: _searchController.text.isNotEmpty
                            ? IconButton(
                                icon: const Icon(Icons.clear_rounded, size: 18),
                                onPressed: () {
                                  _searchController.clear();
                                  ref
                                      .read(scheduleFilterProvider.notifier)
                                      .update(
                                        (state) =>
                                            state.copyWith(searchQuery: ''),
                                      );
                                },
                              )
                            : null,
                      ),
                    ),
                    const SizedBox(height: 14),

                    // Destination filter chips
                    SingleChildScrollView(
                      scrollDirection: Axis.horizontal,
                      child: Row(
                        children: _destinationPresets.map((dest) {
                          final isSelected = dest == 'All'
                              ? (filterState.destination == null ||
                                    filterState.destination!.isEmpty)
                              : filterState.destination == dest;
                          return Padding(
                            padding: const EdgeInsets.only(right: 8),
                            child: ChoiceChip(
                              label: Text(dest),
                              selected: isSelected,
                              onSelected: (_) {
                                ref
                                    .read(scheduleFilterProvider.notifier)
                                    .update(
                                      (state) => dest == 'All'
                                          ? state.copyWith(
                                              clearDestination: true,
                                            )
                                          : state.copyWith(destination: dest),
                                    );
                              },
                              selectedColor: BookingTheme.primary,
                              labelStyle: TextStyle(
                                color: isSelected
                                    ? Colors.white
                                    : BookingTheme.forestDark,
                                fontWeight: FontWeight.w700,
                                fontSize: 13,
                              ),
                              backgroundColor: Colors.white,
                              side: BorderSide(
                                color: isSelected
                                    ? BookingTheme.primary
                                    : BookingTheme.border,
                              ),
                              shape: RoundedRectangleBorder(
                                borderRadius: BorderRadius.circular(12),
                              ),
                            ),
                          );
                        }).toList(),
                      ),
                    ),
                    const SizedBox(height: 10),

                    // Date filter button / active chip
                    Row(
                      children: [
                        OutlinedButton.icon(
                          onPressed: _pickDateFilter,
                          icon: const Icon(
                            Icons.calendar_month_rounded,
                            size: 16,
                          ),
                          label: Text(
                            filterState.filterDate != null
                                ? DateFormat('EEE, d MMM')
                                      .format(filterState.filterDate!)
                                : 'Filter by Date',
                            style: const TextStyle(
                              fontSize: 12,
                              fontWeight: FontWeight.w600,
                            ),
                          ),
                          style: OutlinedButton.styleFrom(
                            foregroundColor: filterState.filterDate != null
                                ? BookingTheme.primary
                                : BookingTheme.forestDark,
                            backgroundColor: filterState.filterDate != null
                                ? BookingTheme.mintLight
                                : Colors.white,
                            side: BorderSide(
                              color: filterState.filterDate != null
                                  ? BookingTheme.primary
                                  : BookingTheme.border,
                            ),
                            shape: RoundedRectangleBorder(
                              borderRadius: BorderRadius.circular(10),
                            ),
                            padding: const EdgeInsets.symmetric(
                              horizontal: 12,
                              vertical: 8,
                            ),
                          ),
                        ),
                        if (filterState.filterDate != null) ...[
                          const SizedBox(width: 8),
                          IconButton(
                            icon: const Icon(Icons.close_rounded, size: 18),
                            tooltip: 'Clear date filter',
                            onPressed: _clearDateFilter,
                          ),
                        ],
                      ],
                    ),
                  ],
                ),
              ),
            ),

            // Schedules List or State Views
            schedulesAsync.when(
              loading: () => const SliverFillRemaining(
                child: LoadingState(message: 'Searching available tours...'),
              ),
              error: (err, _) => SliverFillRemaining(
                child: ErrorState(
                  message: err.toString(),
                  onRetry: () =>
                      ref.read(scheduleListProvider.notifier).loadSchedules(),
                ),
              ),
              data: (schedules) {
                if (schedules.isEmpty) {
                  return SliverFillRemaining(
                    child: EmptyState(
                      title: 'No schedules found',
                      description: 'Try modifying your search keywords or clear the date filters.',
                      icon: Icons.search_off_rounded,
                      actionText: 'Reset Filters',
                      onAction: () {
                        _searchController.clear();
                        ref.read(scheduleFilterProvider.notifier).state =
                            const ScheduleFilterState();
                      },
                    ),
                  );
                }

                return SliverPadding(
                  padding: const EdgeInsets.fromLTRB(20, 8, 20, 32),
                  sliver: SliverList(
                    delegate: SliverChildBuilderDelegate((context, index) {
                      final schedule = schedules[index];
                      return Padding(
                        padding: const EdgeInsets.only(bottom: 16),
                        child: ScheduleCard(
                          schedule: schedule,
                          onSelect: () =>
                              _navigateToSlotSelection(context, schedule),
                        ),
                      );
                    }, childCount: schedules.length),
                  ),
                );
              },
            ),
          ],
        ),
      ),
    );
  }

  void _navigateToSlotSelection(
    BuildContext context,
    BookingSchedule schedule,
  ) {
    Navigator.of(context).push(
      MaterialPageRoute(
        builder: (_) => SlotSelectionScreen(schedule: schedule),
      ),
    );
  }
}
