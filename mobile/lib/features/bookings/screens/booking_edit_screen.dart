import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/intl.dart';

import '../models/booking.dart';
import '../providers/booking_providers.dart';
import '../theme/booking_theme.dart';

class BookingEditScreen extends ConsumerStatefulWidget {
  final Booking booking;

  const BookingEditScreen({super.key, required this.booking});

  @override
  ConsumerState<BookingEditScreen> createState() => _BookingEditScreenState();
}

class _BookingEditScreenState extends ConsumerState<BookingEditScreen> {
  late DateTime _selectedDate;
  late String _selectedSlot;
  late int _guests;
  late final TextEditingController _notesController;

  @override
  void initState() {
    super.initState();
    _selectedDate = widget.booking.date;
    _selectedSlot = widget.booking.timeSlot;
    _guests = widget.booking.guests;
    _notesController = TextEditingController(text: widget.booking.notes ?? '');
  }

  @override
  void dispose() {
    _notesController.dispose();
    super.dispose();
  }

  void _save() {
    final loadedSchedule = ref.read(scheduleListProvider).valueOrNull?.firstWhere(
          (item) => item.id == widget.booking.scheduleId,
        );
    final schedule = loadedSchedule ?? widget.booking.schedule;

    final basePrice = schedule.pricePerPerson * _guests;
    final serviceFee = basePrice * 0.05;
    final discountAmount = _guests > 6 ? basePrice * 0.20 : 0.0;
    final updated = widget.booking.copyWith(
      date: _selectedDate,
      timeSlot: _selectedSlot,
      guests: _guests,
      basePrice: basePrice,
      serviceFee: serviceFee,
      discountAmount: discountAmount,
      totalAmount: basePrice + serviceFee - discountAmount,
      notes: _notesController.text.trim(),
    );

    ref.read(travelerBookingsProvider.notifier).updateBooking(updated);
    Navigator.of(context).pop(true);
  }

  @override
  Widget build(BuildContext context) {
    final loadedSchedule = ref.read(scheduleListProvider).valueOrNull?.firstWhere(
          (item) => item.id == widget.booking.scheduleId,
        );
    final schedule = loadedSchedule ?? widget.booking.schedule;
    final dates = {...schedule.availableDates, _selectedDate}.toList();
    final slots = {...schedule.availableTimeSlots, _selectedSlot}.toList();

    return Scaffold(
      backgroundColor: BookingTheme.background,
      appBar: AppBar(title: const Text('Edit Booking')),
      body: ListView(
        padding: const EdgeInsets.fromLTRB(20, 20, 20, 32),
        children: [
          Text(
            widget.booking.destinationTitle,
            style: const TextStyle(
              fontSize: 22,
              fontWeight: FontWeight.w900,
              color: BookingTheme.forestDark,
            ),
          ),
          const SizedBox(height: 22),
          DropdownButtonFormField<DateTime>(
            initialValue: _selectedDate,
            decoration: const InputDecoration(labelText: 'Travel date'),
            items: dates
                .map(
                  (date) => DropdownMenuItem(
                    value: date,
                    child: Text(DateFormat('EEE, d MMM yyyy').format(date)),
                  ),
                )
                .toList(),
            onChanged: (date) {
              if (date != null) setState(() => _selectedDate = date);
            },
          ),
          const SizedBox(height: 16),
          DropdownButtonFormField<String>(
            initialValue: _selectedSlot,
            decoration: const InputDecoration(labelText: 'Time slot'),
            items: slots
                .map(
                  (slot) => DropdownMenuItem(value: slot, child: Text(slot)),
                )
                .toList(),
            onChanged: (slot) {
              if (slot != null) setState(() => _selectedSlot = slot);
            },
          ),
          const SizedBox(height: 16),
          InputDecorator(
            decoration: const InputDecoration(labelText: 'Guests'),
            child: Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                IconButton(
                  onPressed: _guests > 1
                      ? () => setState(() => _guests--)
                      : null,
                  icon: const Icon(Icons.remove_circle_outline),
                ),
                Text(
                  '$_guests',
                  style: const TextStyle(
                    fontSize: 18,
                    fontWeight: FontWeight.w800,
                  ),
                ),
                IconButton(
                  onPressed: _guests < 20
                      ? () => setState(() => _guests++)
                      : null,
                  icon: const Icon(Icons.add_circle_outline),
                ),
              ],
            ),
          ),
          const SizedBox(height: 16),
          TextFormField(
            controller: _notesController,
            maxLines: 4,
            decoration: const InputDecoration(
              labelText: 'Notes',
              alignLabelWithHint: true,
            ),
          ),
          const SizedBox(height: 28),
          FilledButton.icon(
            onPressed: _save,
            icon: const Icon(Icons.save_outlined),
            label: const Text('Save Changes'),
          ),
        ],
      ),
    );
  }
}

extension on Booking {
  BookingSchedule get schedule => BookingSchedule(
        id: scheduleId,
        destinationId: scheduleId,
        destinationTitle: destinationTitle,
        location: location,
        guideName: '',
        pricePerPerson: basePrice / guests,
        availableDates: [date],
        availableTimeSlots: [timeSlot],
        maxCapacityPerSlot: guests,
        bookedSlotsMap: const {},
        rating: 0,
        reviewsCount: 0,
      );
}
