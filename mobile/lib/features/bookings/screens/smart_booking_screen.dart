import 'dart:async';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/intl.dart';

import '../models/booking.dart';
import '../providers/booking_providers.dart';
import '../services/booking_api_service.dart';
import '../theme/booking_theme.dart';
import 'booking_history_screen.dart';

class SmartBookingScreen extends ConsumerStatefulWidget {
  final String? initialDestinationHint;

  const SmartBookingScreen({super.key, this.initialDestinationHint});

  @override
  ConsumerState<SmartBookingScreen> createState() => _SmartBookingScreenState();
}

class _SmartBookingScreenState extends ConsumerState<SmartBookingScreen> {
  final TextEditingController _objectiveController = TextEditingController();
  Timer? _pollTimer;

  final List<String> _samplePrompts = const [
    'Book Ella Rock for 2 people tomorrow morning',
    'Book Sigiriya fortress tour for 3 guests',
    'Book Mirissa whale watching for 4 people',
  ];

  @override
  void initState() {
    super.initState();
    if (widget.initialDestinationHint != null) {
      _objectiveController.text =
          'Book ${widget.initialDestinationHint} for 2 people next available date';
    }
  }

  @override
  void dispose() {
    _objectiveController.dispose();
    _pollTimer?.cancel();
    super.dispose();
  }

  void _submitObjective() async {
    final text = _objectiveController.text.trim();
    if (text.isEmpty) return;

    FocusScope.of(context).unfocus();

    // Get traveler ID from current session — must be authenticated
    final travelerData = await bookingApiService.getAuthenticatedTraveler();
    final travelerId = travelerData['id'];

    if (travelerId == null || travelerId.isEmpty) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(
            content: Text(
              '⚠️ Please sign in to use the Smart Booking Assistant.',
            ),
            backgroundColor: Color(0xFFE57373),
            behavior: SnackBarBehavior.floating,
          ),
        );
      }
      return;
    }

    final workflow = await ref
        .read(agentBookingProvider.notifier)
        .startWorkflow(
          travelerId: travelerId,
          objective: text,
          travelerName: travelerData['name'] ?? 'Traveler',
          travelerEmail: travelerData['email'] ?? '',
        );

    if (workflow != null && workflow.isPendingApproval) {
      _startPolling(workflow.id);
    }
  }

  void _startPolling(String workflowId) {
    _pollTimer?.cancel();
    _pollTimer = Timer.periodic(const Duration(seconds: 5), (timer) async {
      final updated = await ref
          .read(agentBookingProvider.notifier)
          .pollWorkflow(workflowId);
      if (updated != null && !updated.isPendingApproval) {
        timer.cancel();
        // If completed, refresh the traveler's bookings
        if (updated.isCompleted) {
          ref.read(travelerBookingsProvider.notifier).reload();
        }
      }
    });
  }

  void _checkStatusManually(String workflowId) async {
    final updated = await ref
        .read(agentBookingProvider.notifier)
        .pollWorkflow(workflowId);
    if (updated != null && updated.isCompleted) {
      ref.read(travelerBookingsProvider.notifier).reload();
    }
  }

  @override
  Widget build(BuildContext context) {
    final state = ref.watch(agentBookingProvider);
    final workflow = state.workflow;

    return Scaffold(
      backgroundColor: BookingTheme.background,
      appBar: AppBar(
        title: const Text(
          'Smart Booking Assistant',
          style: TextStyle(
            fontWeight: FontWeight.w900,
            color: BookingTheme.forestDark,
            fontSize: 20,
          ),
        ),
        actions: [
          IconButton(
            tooltip: 'Reset',
            icon: const Icon(Icons.refresh_rounded, color: BookingTheme.primary),
            onPressed: () {
              _pollTimer?.cancel();
              ref.read(agentBookingProvider.notifier).reset();
              _objectiveController.clear();
            },
          ),
        ],
      ),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(20),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            // Header card
            Container(
              padding: const EdgeInsets.all(18),
              decoration: BoxDecoration(
                gradient: const LinearGradient(
                  colors: [BookingTheme.primary, BookingTheme.forestDark],
                  begin: Alignment.topLeft,
                  end: Alignment.bottomRight,
                ),
                borderRadius: BorderRadius.circular(20),
                boxShadow: [
                  BoxShadow(
                    color: BookingTheme.primary.withValues(alpha: 0.25),
                    blurRadius: 16,
                    offset: const Offset(0, 6),
                  ),
                ],
              ),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Row(
                    children: [
                      Container(
                        padding: const EdgeInsets.all(8),
                        decoration: BoxDecoration(
                          color: Colors.white.withValues(alpha: 0.2),
                          borderRadius: BorderRadius.circular(10),
                        ),
                        child: const Icon(
                          Icons.auto_awesome,
                          color: Colors.white,
                          size: 22,
                        ),
                      ),
                      const SizedBox(width: 12),
                      const Expanded(
                        child: Text(
                          'Agentic Booking Assistant',
                          style: TextStyle(
                            color: Colors.white,
                            fontSize: 18,
                            fontWeight: FontWeight.w800,
                          ),
                        ),
                      ),
                    ],
                  ),
                  const SizedBox(height: 10),
                  Text(
                    'State your travel objective in plain words. The agent inspects live schedules, validates slot capacity, guards against duplicate bookings, and secures your booking with financial escrow.',
                    style: TextStyle(
                      color: Colors.white.withValues(alpha: 0.9),
                      fontSize: 13,
                      height: 1.4,
                    ),
                  ),
                ],
              ),
            ),
            const SizedBox(height: 20),

            // Objective Input Card
            Container(
              padding: const EdgeInsets.all(16),
              decoration: BoxDecoration(
                color: Colors.white,
                borderRadius: BorderRadius.circular(18),
                border: Border.all(color: BookingTheme.border),
              ),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  const Text(
                    'What would you like to book?',
                    style: TextStyle(
                      fontWeight: FontWeight.w800,
                      fontSize: 15,
                      color: BookingTheme.forestDark,
                    ),
                  ),
                  const SizedBox(height: 10),
                  TextField(
                    controller: _objectiveController,
                    maxLines: 3,
                    enabled: !state.isLoading,
                    decoration: InputDecoration(
                      hintText:
                          'e.g. "I want to book the Ella Rock trek for 2 people tomorrow morning"',
                      hintStyle: TextStyle(
                        color: BookingTheme.forestDark.withValues(alpha: 0.4),
                        fontSize: 13,
                      ),
                      filled: true,
                      fillColor: BookingTheme.background,
                      border: OutlineInputBorder(
                        borderRadius: BorderRadius.circular(12),
                        borderSide: BorderSide.none,
                      ),
                      contentPadding: const EdgeInsets.all(12),
                    ),
                  ),
                  const SizedBox(height: 12),

                  // Quick prompts chips
                  Wrap(
                    spacing: 8,
                    runSpacing: 8,
                    children: _samplePrompts.map((prompt) {
                      return ActionChip(
                        label: Text(
                          prompt,
                          style: const TextStyle(fontSize: 11),
                        ),
                        backgroundColor: BookingTheme.background,
                        side: const BorderSide(color: BookingTheme.border),
                        onPressed: state.isLoading
                            ? null
                            : () {
                                _objectiveController.text = prompt;
                              },
                      );
                    }).toList(),
                  ),
                  const SizedBox(height: 16),

                  SizedBox(
                    width: double.infinity,
                    child: ElevatedButton.icon(
                      style: ElevatedButton.styleFrom(
                        backgroundColor: BookingTheme.primary,
                        foregroundColor: Colors.white,
                        padding: const EdgeInsets.symmetric(vertical: 14),
                        shape: RoundedRectangleBorder(
                          borderRadius: BorderRadius.circular(12),
                        ),
                      ),
                      onPressed: state.isLoading ? null : _submitObjective,
                      icon: state.isLoading
                          ? const SizedBox(
                              width: 18,
                              height: 18,
                              child: CircularProgressIndicator(
                                strokeWidth: 2,
                                color: Colors.white,
                              ),
                            )
                          : const Icon(Icons.bolt_rounded, size: 20),
                      label: Text(
                        state.isLoading
                            ? 'Agent is Planning & Checking...'
                            : 'Plan & Verify Booking',
                        style: const TextStyle(fontWeight: FontWeight.w800),
                      ),
                    ),
                  ),
                ],
              ),
            ),
            const SizedBox(height: 20),

            // Error display
            if (state.error != null)
              Container(
                margin: const EdgeInsets.only(bottom: 20),
                padding: const EdgeInsets.all(14),
                decoration: BoxDecoration(
                  color: const Color(0xFFFEF2F2),
                  border: Border.all(color: const Color(0xFFFECACA)),
                  borderRadius: BorderRadius.circular(14),
                ),
                child: Row(
                  children: [
                    const Icon(Icons.error_outline, color: Color(0xFFDC2626)),
                    const SizedBox(width: 12),
                    Expanded(
                      child: Text(
                        state.error!,
                        style: const TextStyle(
                          color: Color(0xFF991B1B),
                          fontSize: 13,
                        ),
                      ),
                    ),
                  ],
                ),
              ),

            // Agent Result / Proposal Card
            if (workflow != null) _buildWorkflowCard(workflow),
          ],
        ),
      ),
    );
  }

  Widget _buildWorkflowCard(AgentWorkflow wf) {
    final prop = wf.proposedBooking;

    Color statusColor;
    String statusTitle;
    IconData statusIcon;

    if (wf.isCompleted) {
      statusColor = const Color(0xFF10B981);
      statusTitle = 'Booking Confirmed';
      statusIcon = Icons.check_circle_rounded;
    } else if (wf.isPendingApproval) {
      statusColor = const Color(0xFFF59E0B);
      statusTitle = 'Awaiting Operator Approval';
      statusIcon = Icons.hourglass_top_rounded;
    } else if (wf.needsMoreInfo) {
      statusColor = const Color(0xFFD97706);
      statusTitle = 'A Few More Details Needed';
      statusIcon = Icons.info_outline_rounded;
    } else if (wf.isRejected) {
      statusColor = const Color(0xFFEF4444);
      statusTitle = 'Proposal Declined';
      statusIcon = Icons.cancel_rounded;
    } else {
      statusColor = const Color(0xFFEF4444);
      statusTitle = 'Validation Failed';
      statusIcon = Icons.error_rounded;
    }

    return Container(
      padding: const EdgeInsets.all(18),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(18),
        border: Border.all(color: BookingTheme.border),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withValues(alpha: 0.04),
            blurRadius: 10,
            offset: const Offset(0, 4),
          ),
        ],
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          // Status badge row
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Container(
                padding:
                    const EdgeInsets.symmetric(horizontal: 10, vertical: 5),
                decoration: BoxDecoration(
                  color: statusColor.withValues(alpha: 0.12),
                  borderRadius: BorderRadius.circular(8),
                ),
                child: Row(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    Icon(statusIcon, color: statusColor, size: 16),
                    const SizedBox(width: 6),
                    Text(
                      statusTitle,
                      style: TextStyle(
                        color: statusColor,
                        fontWeight: FontWeight.w800,
                        fontSize: 12,
                      ),
                    ),
                  ],
                ),
              ),
              if (wf.bookingReference != null)
                Text(
                  wf.bookingReference!,
                  style: const TextStyle(
                    fontWeight: FontWeight.w900,
                    color: BookingTheme.primary,
                  ),
                ),
            ],
          ),
          const SizedBox(height: 14),

          if (prop != null) ...[
            Text(
              prop.destinationTitle,
              style: const TextStyle(
                fontSize: 18,
                fontWeight: FontWeight.w900,
                color: BookingTheme.forestDark,
              ),
            ),
            const SizedBox(height: 4),
            Text(
              prop.location,
              style: TextStyle(
                fontSize: 13,
                color: BookingTheme.forestDark.withValues(alpha: 0.6),
              ),
            ),
            const SizedBox(height: 14),

            // Proposal breakdown table
            Container(
              padding: const EdgeInsets.all(12),
              decoration: BoxDecoration(
                color: BookingTheme.background,
                borderRadius: BorderRadius.circular(12),
              ),
              child: Column(
                children: [
                  _summaryRow(
                    'Date & Time',
                    '${DateFormat('dd MMM yyyy').format(prop.bookingDate)} (${prop.timeSlot})',
                  ),
                  _summaryRow('Guests', '${prop.guests} person(s)'),
                  _summaryRow(
                    'Rate per person',
                    'LKR ${prop.pricePerPerson.toStringAsFixed(0)}',
                  ),
                  _summaryRow(
                    'Base price',
                    'LKR ${prop.basePrice.toStringAsFixed(0)}',
                  ),
                  _summaryRow(
                    'Service fee (5%)',
                    'LKR ${prop.serviceFee.toStringAsFixed(0)}',
                  ),
                  if (prop.discountAmount > 0)
                    _summaryRow(
                      'Group discount',
                      '- LKR ${prop.discountAmount.toStringAsFixed(0)}',
                      valueColor: const Color(0xFF10B981),
                    ),
                  const Divider(height: 16),
                  _summaryRow(
                    'Total Escrow Hold',
                    'LKR ${prop.totalAmount.toStringAsFixed(0)}',
                    isTotal: true,
                  ),
                ],
              ),
            ),
            const SizedBox(height: 14),
          ],

          if (wf.errorMessage != null && wf.isFailed) ...[
            Container(
              padding: const EdgeInsets.all(14),
              decoration: BoxDecoration(
                color: const Color(0xFFFFF7ED),
                border: Border.all(color: const Color(0xFFFED7AA)),
                borderRadius: BorderRadius.circular(14),
              ),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Row(
                    children: [
                      Container(
                        padding: const EdgeInsets.all(6),
                        decoration: BoxDecoration(
                          color: const Color(0xFFFEF3C7),
                          borderRadius: BorderRadius.circular(8),
                        ),
                        child: const Icon(
                          Icons.sentiment_neutral_rounded,
                          color: Color(0xFFD97706),
                          size: 20,
                        ),
                      ),
                      const SizedBox(width: 10),
                      Text(
                        wf.needsMoreInfo
                            ? 'Please Complete Your Request'
                            : 'Unable to Process Request',
                        style: TextStyle(
                          fontWeight: FontWeight.w800,
                          fontSize: 14,
                          color: Color(0xFF92400E),
                        ),
                      ),
                    ],
                  ),
                  const SizedBox(height: 10),
                  Text(
                    wf.errorMessage!,
                    style: const TextStyle(
                      color: Color(0xFF92400E),
                      fontSize: 13,
                      height: 1.5,
                    ),
                  ),
                  const SizedBox(height: 10),
                  const Row(
                    children: [
                      Icon(Icons.lightbulb_outline,
                          size: 14, color: Color(0xFFD97706)),
                      SizedBox(width: 6),
                      Expanded(
                        child: Text(
                          'Include the tour, number of travelers, date, and time. If flexible, say “next available date” or “any available time.”',
                          style: TextStyle(
                            fontSize: 12,
                            color: Color(0xFFB45309),
                            fontStyle: FontStyle.italic,
                          ),
                        ),
                      ),
                    ],
                  ),
                ],
              ),
            ),
            const SizedBox(height: 14),
          ],

          // Steps list expansion
          Theme(
            data: Theme.of(context).copyWith(dividerColor: Colors.transparent),
            child: ExpansionTile(
              tilePadding: EdgeInsets.zero,
              title: Text(
                'Agent Plan & Audit Trail (${wf.completedSteps.length}/${wf.plan.length} steps)',
                style: const TextStyle(
                  fontSize: 13,
                  fontWeight: FontWeight.w700,
                  color: BookingTheme.forestDark,
                ),
              ),
              children: wf.completedSteps.map((step) {
                return Padding(
                  padding: const EdgeInsets.symmetric(vertical: 3),
                  child: Row(
                    children: [
                      const Icon(
                        Icons.check_circle_rounded,
                        color: BookingTheme.primary,
                        size: 16,
                      ),
                      const SizedBox(width: 8),
                      Expanded(
                        child: Text(
                          step,
                          style: const TextStyle(fontSize: 12, height: 1.3),
                        ),
                      ),
                    ],
                  ),
                );
              }).toList(),
            ),
          ),
          const SizedBox(height: 14),

          // Actions based on state
          if (wf.isPendingApproval) ...[
            SizedBox(
              width: double.infinity,
              child: OutlinedButton.icon(
                style: OutlinedButton.styleFrom(
                  foregroundColor: BookingTheme.primary,
                  side: const BorderSide(color: BookingTheme.primary),
                  padding: const EdgeInsets.symmetric(vertical: 12),
                  shape: RoundedRectangleBorder(
                    borderRadius: BorderRadius.circular(10),
                  ),
                ),
                onPressed: () => _checkStatusManually(wf.id),
                icon: const Icon(Icons.refresh_rounded, size: 18),
                label: const Text(
                  'Check Admin Approval Status',
                  style: TextStyle(fontWeight: FontWeight.w700),
                ),
              ),
            ),
          ] else if (wf.isCompleted) ...[
            SizedBox(
              width: double.infinity,
              child: ElevatedButton.icon(
                style: ElevatedButton.styleFrom(
                  backgroundColor: const Color(0xFF10B981),
                  foregroundColor: Colors.white,
                  padding: const EdgeInsets.symmetric(vertical: 12),
                  shape: RoundedRectangleBorder(
                    borderRadius: BorderRadius.circular(10),
                  ),
                ),
                onPressed: () {
                  Navigator.of(context).push(
                    MaterialPageRoute(
                      builder: (_) => const BookingHistoryScreen(),
                    ),
                  );
                },
                icon: const Icon(Icons.receipt_long_rounded, size: 18),
                label: const Text(
                  'View in My Bookings',
                  style: TextStyle(fontWeight: FontWeight.w800),
                ),
              ),
            ),
          ],
        ],
      ),
    );
  }

  Widget _summaryRow(
    String label,
    String value, {
    Color? valueColor,
    bool isTotal = false,
  }) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 3),
      child: Row(
        mainAxisAlignment: MainAxisAlignment.spaceBetween,
        children: [
          Text(
            label,
            style: TextStyle(
              fontSize: isTotal ? 14 : 12,
              fontWeight: isTotal ? FontWeight.w800 : FontWeight.w500,
              color: BookingTheme.forestDark,
            ),
          ),
          Text(
            value,
            style: TextStyle(
              fontSize: isTotal ? 15 : 12,
              fontWeight: isTotal ? FontWeight.w900 : FontWeight.w700,
              color: valueColor ??
                  (isTotal ? BookingTheme.primary : BookingTheme.forestDark),
            ),
          ),
        ],
      ),
    );
  }
}
