import 'package:flutter/material.dart';
import 'package:intl/intl.dart';
import '../models/booking.dart';
import '../theme/booking_theme.dart';
import '../widgets/primary_gradient_button.dart';
import 'booking_checkout_screen.dart';

class SlotSelectionScreen extends StatefulWidget {
  final BookingSchedule schedule;

  const SlotSelectionScreen({
    super.key,
    required this.schedule,
  });

  @override
  State<SlotSelectionScreen> createState() => _SlotSelectionScreenState();
}

class _SlotSelectionScreenState extends State<SlotSelectionScreen> {
  late DateTime _selectedDate;
  String? _selectedSlot;
  int _guests = 1;
  String? _validationError;

  @override
  void initState() {
    super.initState();
    _selectedDate = widget.schedule.availableDates.isNotEmpty
        ? widget.schedule.availableDates.first
        : DateTime.now();

    // Pick first non-full slot if available
    for (final slot in widget.schedule.availableTimeSlots) {
      if (!widget.schedule.isSlotFull(_selectedDate, slot)) {
        _selectedSlot = slot;
        break;
      }
    }
  }

  Future<void> _pickCustomDate() async {
    final picked = await showDatePicker(
      context: context,
      initialDate: _selectedDate,
      firstDate: DateTime.now(),
      lastDate: DateTime.now().add(const Duration(days: 60)),
      selectableDayPredicate: (day) {
        return widget.schedule.availableDates.any((d) =>
            d.year == day.year && d.month == day.month && d.day == day.day);
      },
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
      setState(() {
        _selectedDate = picked;
        _selectedSlot = null; // reset slot on date change
        _validationError = null;
      });
    }
  }

  void _onProceedToCheckout() {
    setState(() => _validationError = null);

    if (_selectedSlot == null) {
      setState(() => _validationError = 'Please select a tour time slot.');
      return;
    }

    final remaining = widget.schedule.remainingCapacity(_selectedDate, _selectedSlot!);
    if (remaining <= 0) {
      setState(() => _validationError = 'The selected time slot is full. Choose another slot.');
      return;
    }

    if (_guests > remaining) {
      setState(() => _validationError =
          'Only $remaining spot(s) available for this slot. Please reduce guests.');
      return;
    }

    Navigator.of(context).push(
      MaterialPageRoute(
        builder: (_) => BookingCheckoutScreen(
          schedule: widget.schedule,
          selectedDate: _selectedDate,
          selectedSlot: _selectedSlot!,
          guests: _guests,
        ),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final currencyFormatter = NumberFormat.currency(
      symbol: 'LKR ',
      decimalDigits: 0,
    );

    final currentRemaining = _selectedSlot != null
        ? widget.schedule.remainingCapacity(_selectedDate, _selectedSlot!)
        : widget.schedule.maxCapacityPerSlot;

    final isSlotFull = _selectedSlot != null &&
        widget.schedule.isSlotFull(_selectedDate, _selectedSlot!, requestedGuests: _guests);

    return Scaffold(
      backgroundColor: BookingTheme.background,
      appBar: AppBar(
        title: const Text('Select Date & Time'),
        centerTitle: false,
      ),
      body: SingleChildScrollView(
        padding: const EdgeInsets.fromLTRB(20, 16, 20, 120),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // Tour Summary Banner
            Container(
              padding: const EdgeInsets.all(18),
              decoration: BoxDecoration(
                color: Colors.white,
                borderRadius: BorderRadius.circular(20),
                border: Border.all(color: BookingTheme.border),
              ),
              child: Row(
                children: [
                  Container(
                    width: 50,
                    height: 50,
                    decoration: BoxDecoration(
                      color: BookingTheme.mintLight,
                      borderRadius: BorderRadius.circular(14),
                    ),
                    child: const Icon(
                      Icons.tour_rounded,
                      color: BookingTheme.primary,
                      size: 26,
                    ),
                  ),
                  const SizedBox(width: 14),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          widget.schedule.destinationTitle,
                          style: const TextStyle(
                            fontSize: 16,
                            fontWeight: FontWeight.w800,
                            color: BookingTheme.forestDark,
                          ),
                          maxLines: 1,
                          overflow: TextOverflow.ellipsis,
                        ),
                        const SizedBox(height: 4),
                        Text(
                          '${widget.schedule.location} • Guide: ${widget.schedule.guideName}',
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
            ),
            const SizedBox(height: 24),

            // Date Selection
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                const Text(
                  'Select Tour Date',
                  style: TextStyle(
                    fontSize: 18,
                    fontWeight: FontWeight.w800,
                    color: BookingTheme.forestDark,
                  ),
                ),
                TextButton.icon(
                  onPressed: _pickCustomDate,
                  icon: const Icon(Icons.calendar_month_outlined, size: 16),
                  label: const Text('Calendar'),
                  style: TextButton.styleFrom(
                    foregroundColor: BookingTheme.primary,
                    textStyle: const TextStyle(fontSize: 13, fontWeight: FontWeight.w700),
                  ),
                ),
              ],
            ),
            const SizedBox(height: 12),

            // Horizontal Date Chips
            SizedBox(
              height: 86,
              child: ListView.separated(
                scrollDirection: Axis.horizontal,
                itemCount: widget.schedule.availableDates.length,
                separatorBuilder: (_, _) => const SizedBox(width: 12),
                itemBuilder: (context, index) {
                  final date = widget.schedule.availableDates[index];
                  final isSelected = date.year == _selectedDate.year &&
                      date.month == _selectedDate.month &&
                      date.day == _selectedDate.day;

                  return GestureDetector(
                    onTap: () {
                      setState(() {
                        _selectedDate = date;
                        _selectedSlot = null;
                        _validationError = null;
                      });
                    },
                    child: Container(
                      width: 76,
                      padding: const EdgeInsets.symmetric(vertical: 6, horizontal: 4),
                      decoration: BoxDecoration(
                        color: isSelected ? BookingTheme.primary : Colors.white,
                        borderRadius: BorderRadius.circular(16),
                        border: Border.all(
                          color: isSelected ? BookingTheme.primary : BookingTheme.border,
                          width: isSelected ? 2 : 1,
                        ),
                        boxShadow: isSelected
                            ? [
                                BoxShadow(
                                  color: BookingTheme.primary.withValues(alpha: 0.25),
                                  blurRadius: 10,
                                  offset: const Offset(0, 4),
                                ),
                              ]
                            : null,
                      ),
                      child: Column(
                        mainAxisAlignment: MainAxisAlignment.center,
                        mainAxisSize: MainAxisSize.min,
                        children: [
                          Text(
                            DateFormat('EEE').format(date).toUpperCase(),
                            style: TextStyle(
                              fontSize: 11,
                              fontWeight: FontWeight.w700,
                              color: isSelected ? Colors.white70 : BookingTheme.textMuted,
                            ),
                          ),
                          const SizedBox(height: 2),
                          Text(
                            DateFormat('d').format(date),
                            style: TextStyle(
                              fontSize: 20,
                              fontWeight: FontWeight.w900,
                              color: isSelected ? Colors.white : BookingTheme.forestDark,
                            ),
                          ),
                          Text(
                            DateFormat('MMM').format(date),
                            style: TextStyle(
                              fontSize: 11,
                              fontWeight: FontWeight.w600,
                              color: isSelected ? Colors.white70 : BookingTheme.textMuted,
                            ),
                          ),
                        ],
                      ),
                    ),
                  );
                },
              ),
            ),
            const SizedBox(height: 28),

            // Time Slot Selection (Disables full slots)
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                const Text(
                  'Available Time Slots',
                  style: TextStyle(
                    fontSize: 18,
                    fontWeight: FontWeight.w800,
                    color: BookingTheme.forestDark,
                  ),
                ),
                Text(
                  'Cap: ${widget.schedule.maxCapacityPerSlot}/slot',
                  style: const TextStyle(
                    fontSize: 12,
                    fontWeight: FontWeight.w600,
                    color: BookingTheme.textMuted,
                  ),
                ),
              ],
            ),
            const SizedBox(height: 12),

            // Slots Grid/Wrap
            Column(
              children: widget.schedule.availableTimeSlots.map((slot) {
                final remaining = widget.schedule.remainingCapacity(_selectedDate, slot);
                final isFull = remaining <= 0;
                final isSelected = _selectedSlot == slot;

                return Padding(
                  padding: const EdgeInsets.only(bottom: 10),
                  child: InkWell(
                    onTap: isFull
                        ? null
                        : () {
                            setState(() {
                              _selectedSlot = slot;
                              _validationError = null;
                              if (_guests > remaining) {
                                _guests = remaining > 0 ? remaining : 1;
                              }
                            });
                          },
                    borderRadius: BorderRadius.circular(16),
                    child: Container(
                      padding: const EdgeInsets.symmetric(horizontal: 18, vertical: 14),
                      decoration: BoxDecoration(
                        color: isFull
                            ? const Color(0xFFF0ECEA)
                            : (isSelected ? BookingTheme.mintLight : Colors.white),
                        borderRadius: BorderRadius.circular(16),
                        border: Border.all(
                          color: isFull
                              ? const Color(0xFFE0DCD8)
                              : (isSelected ? BookingTheme.primary : BookingTheme.border),
                          width: isSelected ? 2 : 1,
                        ),
                      ),
                      child: Row(
                        children: [
                          Icon(
                            isFull
                                ? Icons.block_rounded
                                : (isSelected
                                    ? Icons.radio_button_checked_rounded
                                    : Icons.radio_button_off_rounded),
                            size: 20,
                            color: isFull
                                ? BookingTheme.textMuted
                                : (isSelected ? BookingTheme.primary : BookingTheme.textMuted),
                          ),
                          const SizedBox(width: 12),
                          Text(
                            slot,
                            style: TextStyle(
                              fontSize: 15,
                              fontWeight: isSelected ? FontWeight.w800 : FontWeight.w700,
                              color: isFull
                                  ? BookingTheme.textMuted
                                  : BookingTheme.forestDark,
                              decoration: isFull ? TextDecoration.lineThrough : null,
                            ),
                          ),
                          const Spacer(),
                          // Capacity indicator badge
                          Container(
                            padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
                            decoration: BoxDecoration(
                              color: isFull
                                  ? BookingTheme.errorRed.withValues(alpha: 0.1)
                                  : (remaining <= 2
                                      ? BookingTheme.warningOrange.withValues(alpha: 0.12)
                                      : BookingTheme.primary.withValues(alpha: 0.1)),
                              borderRadius: BorderRadius.circular(10),
                            ),
                            child: Text(
                              isFull
                                  ? 'FULL'
                                  : (remaining <= 2
                                      ? 'Only $remaining left'
                                      : '$remaining spots left'),
                              style: TextStyle(
                                fontSize: 11,
                                fontWeight: FontWeight.w700,
                                color: isFull
                                    ? BookingTheme.errorRed
                                    : (remaining <= 2
                                        ? BookingTheme.warningOrange
                                        : BookingTheme.primary),
                              ),
                            ),
                          ),
                        ],
                      ),
                    ),
                  ),
                );
              }).toList(),
            ),
            const SizedBox(height: 24),

            // Number of Guests
            const Text(
              'Number of Travelers',
              style: TextStyle(
                fontSize: 18,
                fontWeight: FontWeight.w800,
                color: BookingTheme.forestDark,
              ),
            ),
            const SizedBox(height: 12),
            Container(
              padding: const EdgeInsets.symmetric(horizontal: 18, vertical: 12),
              decoration: BoxDecoration(
                color: Colors.white,
                borderRadius: BorderRadius.circular(18),
                border: Border.all(color: BookingTheme.border),
              ),
              child: Row(
                children: [
                  const Icon(Icons.group_outlined, color: BookingTheme.forestDark),
                  const SizedBox(width: 12),
                  const Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        'Guests',
                        style: TextStyle(
                          fontSize: 15,
                          fontWeight: FontWeight.w700,
                          color: BookingTheme.forestDark,
                        ),
                      ),
                      Text(
                        'Ages 12+ standard seat',
                        style: TextStyle(fontSize: 11, color: BookingTheme.textMuted),
                      ),
                    ],
                  ),
                  const Spacer(),
                  IconButton(
                    onPressed: _guests > 1 ? () => setState(() => _guests--) : null,
                    icon: const Icon(Icons.remove_circle_outline_rounded),
                    color: BookingTheme.primary,
                  ),
                  Text(
                    '$_guests',
                    style: const TextStyle(
                      fontSize: 18,
                      fontWeight: FontWeight.w900,
                      color: BookingTheme.forestDark,
                    ),
                  ),
                  IconButton(
                    onPressed: (_selectedSlot != null && _guests < currentRemaining)
                        ? () => setState(() => _guests++)
                        : null,
                    icon: const Icon(Icons.add_circle_outline_rounded),
                    color: BookingTheme.primary,
                  ),
                ],
              ),
            ),

            if (_validationError != null) ...[
              const SizedBox(height: 16),
              Container(
                padding: const EdgeInsets.all(12),
                decoration: BoxDecoration(
                  color: BookingTheme.errorRed.withValues(alpha: 0.1),
                  borderRadius: BorderRadius.circular(12),
                  border: Border.all(color: BookingTheme.errorRed.withValues(alpha: 0.3)),
                ),
                child: Row(
                  children: [
                    const Icon(Icons.error_outline_rounded,
                        color: BookingTheme.errorRed, size: 18),
                    const SizedBox(width: 8),
                    Expanded(
                      child: Text(
                        _validationError!,
                        style: const TextStyle(
                          color: BookingTheme.errorRed,
                          fontSize: 13,
                          fontWeight: FontWeight.w600,
                        ),
                      ),
                    ),
                  ],
                ),
              ),
            ],
          ],
        ),
      ),
      bottomNavigationBar: Container(
        padding: const EdgeInsets.fromLTRB(20, 14, 20, 24),
        decoration: BoxDecoration(
          color: Colors.white,
          border: const Border(top: BorderSide(color: BookingTheme.border)),
          boxShadow: [
            BoxShadow(
              color: Colors.black.withValues(alpha: 0.05),
              blurRadius: 10,
              offset: const Offset(0, -4),
            ),
          ],
        ),
        child: Row(
          children: [
            Column(
              mainAxisSize: MainAxisSize.min,
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const Text(
                  'Estimated Total',
                  style: TextStyle(fontSize: 12, color: BookingTheme.textMuted),
                ),
                Text(
                  currencyFormatter.format(widget.schedule.pricePerPerson * _guests),
                  style: const TextStyle(
                    fontSize: 20,
                    fontWeight: FontWeight.w900,
                    color: BookingTheme.forestDark,
                  ),
                ),
              ],
            ),
            const SizedBox(width: 20),
            Expanded(
              child: PrimaryGradientButton(
                text: 'Proceed to Checkout',
                onPressed: isSlotFull || _selectedSlot == null
                    ? null
                    : _onProceedToCheckout,
              ),
            ),
          ],
        ),
      ),
    );
  }
}
