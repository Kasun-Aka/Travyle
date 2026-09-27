import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/intl.dart';

import '../models/booking.dart';
import '../providers/booking_providers.dart';
import '../theme/booking_theme.dart';
import '../widgets/primary_gradient_button.dart';

class DiscountRequestScreen extends ConsumerStatefulWidget {
  final Booking booking;

  const DiscountRequestScreen({super.key, required this.booking});

  @override
  ConsumerState<DiscountRequestScreen> createState() =>
      _DiscountRequestScreenState();
}

class _DiscountRequestScreenState extends ConsumerState<DiscountRequestScreen> {
  final _formKey = GlobalKey<FormState>();
  final TextEditingController _percentController = TextEditingController(
    text: '10',
  );
  final TextEditingController _reasonController = TextEditingController();
  final TextEditingController _proofController = TextEditingController();

  String _selectedReasonCategory = 'Student Concession';

  // Each category maps to an evidence hint shown below the dropdown
  static const Map<String, String> _categoryEvidenceHints = {
    'Student Concession':
        'Evidence: Valid student ID or university enrollment letter',
    'Group Booking Discount':
        'Evidence: Group of 5+ travelers on the same booking',
    'Senior Citizen Discount': 'Evidence: Proof of age 60+ (NIC, passport)',
    'Financial Hardship':
        'Evidence: A brief written explanation; admin will review',
    'Promotional Campaign Match':
        'Evidence: Screenshot or reference of the external promotion',
  };

  final List<String> _reasonCategories = const [
    'Student Concession',
    'Group Booking Discount',
    'Senior Citizen Discount',
    'Financial Hardship',
    'Promotional Campaign Match',
  ];

  @override
  void dispose() {
    _percentController.dispose();
    _reasonController.dispose();
    _proofController.dispose();
    super.dispose();
  }

  void _submitDiscountRequest() async {
    if (!_formKey.currentState!.validate()) return;

    final percent = double.tryParse(_percentController.text.trim()) ?? 10.0;
    final proof = _proofController.text.trim();
    final fullReason =
        '[$_selectedReasonCategory] Proof: $proof. ${_reasonController.text.trim()}';

    final result = await ref
        .read(createDiscountRequestProvider.notifier)
        .submitRequest(
          bookingId: widget.booking.id,
          scheduleId: widget.booking.scheduleId,
          originalPrice: widget.booking.totalAmount,
          requestedDiscountPercent: percent,
          reason: fullReason,
        );

    if (result != null && mounted) {
      _showConfirmationSheet(context, result);
    }
  }

  void _showConfirmationSheet(BuildContext context, DiscountRequest req) {
    final currencyFormatter = NumberFormat.currency(
      symbol: 'LKR ',
      decimalDigits: 2,
    );

    showModalBottomSheet(
      context: context,
      isScrollControlled: true,
      backgroundColor: Colors.white,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(28)),
      ),
      builder: (bottomSheetCtx) {
        return Padding(
          padding: const EdgeInsets.fromLTRB(24, 20, 24, 40),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              Container(
                width: 44,
                height: 4,
                decoration: BoxDecoration(
                  color: BookingTheme.border,
                  borderRadius: BorderRadius.circular(2),
                ),
              ),
              const SizedBox(height: 24),
              Container(
                padding: const EdgeInsets.all(16),
                decoration: BoxDecoration(
                  color: BookingTheme.mintLight,
                  shape: BoxShape.circle,
                ),
                child: const Icon(
                  Icons.mark_email_read_rounded,
                  color: BookingTheme.primary,
                  size: 40,
                ),
              ),
              const SizedBox(height: 18),
              const Text(
                'Request Submitted for Review',
                style: TextStyle(
                  fontSize: 20,
                  fontWeight: FontWeight.w800,
                  color: BookingTheme.forestDark,
                ),
              ),
              const SizedBox(height: 8),
              Text(
                'Your request for ${req.requestedDiscountPercent.toStringAsFixed(0)}% discount '
                '(${currencyFormatter.format(req.calculatedDiscountAmount)}) is pending admin review. '
                'If approved, the discount will be refunded after your payment is verified.',
                textAlign: TextAlign.center,
                style: const TextStyle(
                  fontSize: 14,
                  color: BookingTheme.textMuted,
                  height: 1.4,
                ),
              ),
              const SizedBox(height: 24),
              PrimaryGradientButton(
                width: double.infinity,
                height: 52,
                text: 'Return to Booking',
                onPressed: () {
                  Navigator.of(bottomSheetCtx).pop(); // close sheet
                  Navigator.of(context).pop(); // return to detail
                },
              ),
            ],
          ),
        );
      },
    );
  }

  Widget _buildCriteriaRow(IconData icon, String title, String subtitle) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 8),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Icon(icon, size: 16, color: BookingTheme.primary),
          const SizedBox(width: 8),
          Expanded(
            child: RichText(
              text: TextSpan(
                children: [
                  TextSpan(
                    text: '$title  ',
                    style: const TextStyle(
                      fontSize: 12,
                      fontWeight: FontWeight.w700,
                      color: BookingTheme.forestDark,
                    ),
                  ),
                  TextSpan(
                    text: subtitle,
                    style: const TextStyle(
                      fontSize: 11,
                      color: BookingTheme.textMuted,
                    ),
                  ),
                ],
              ),
            ),
          ),
        ],
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final currencyFormatter = NumberFormat.currency(
      symbol: 'LKR ',
      decimalDigits: 2,
    );

    final reqState = ref.watch(createDiscountRequestProvider);

    return Scaffold(
      backgroundColor: BookingTheme.background,
      appBar: AppBar(title: const Text('Request Concession / Discount')),
      body: LayoutBuilder(
        builder: (context, constraints) {
          final horizontalPadding = constraints.maxWidth < 360 ? 14.0 : 20.0;
          return SingleChildScrollView(
            padding: EdgeInsets.symmetric(
              horizontal: horizontalPadding,
              vertical: 20,
            ),
            child: Center(
              child: ConstrainedBox(
                constraints: const BoxConstraints(maxWidth: 640),
                child: Form(
                  key: _formKey,
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      // Eligibility Criteria Card
                      Container(
                        padding: const EdgeInsets.all(16),
                        decoration: BoxDecoration(
                          gradient: LinearGradient(
                            colors: [
                              BookingTheme.primary.withValues(alpha: 0.07),
                              BookingTheme.mintLight,
                            ],
                            begin: Alignment.topLeft,
                            end: Alignment.bottomRight,
                          ),
                          borderRadius: BorderRadius.circular(18),
                          border: Border.all(
                            color: BookingTheme.primary.withValues(alpha: 0.2),
                          ),
                        ),
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            const Row(
                              children: [
                                Icon(
                                  Icons.verified_user_outlined,
                                  color: BookingTheme.primary,
                                  size: 20,
                                ),
                                SizedBox(width: 8),
                                Text(
                                  'Who can apply?',
                                  style: TextStyle(
                                    fontSize: 15,
                                    fontWeight: FontWeight.w800,
                                    color: BookingTheme.spruce,
                                  ),
                                ),
                              ],
                            ),
                            const SizedBox(height: 12),
                            _buildCriteriaRow(
                              Icons.school_outlined,
                              'Students',
                              'Up to 15% off with valid student ID',
                            ),
                            _buildCriteriaRow(
                              Icons.group_outlined,
                              'Groups (5+ guests)',
                              'Up to 20% off on a single booking',
                            ),
                            _buildCriteriaRow(
                              Icons.elderly_outlined,
                              'Senior Citizens (60+)',
                              'Up to 15% off with age proof',
                            ),
                            _buildCriteriaRow(
                              Icons.volunteer_activism_outlined,
                              'Financial Hardship',
                              'Up to 10% — reviewed by admin',
                            ),
                            _buildCriteriaRow(
                              Icons.campaign_outlined,
                              'Promo Campaign Match',
                              'Match an active external offer',
                            ),
                            const Divider(
                              height: 20,
                              color: BookingTheme.border,
                            ),
                            const Text(
                              '• Discounts ≤ 15% are auto-approved and applied instantly\n'
                              '• Every request is reviewed by an administrator\n'
                              '• Approved requests are refunded after payment; rejected requests change nothing\n'
                              '• Only one discount request is allowed per booking',
                              style: TextStyle(
                                fontSize: 11,
                                color: BookingTheme.textMuted,
                                height: 1.5,
                              ),
                            ),
                          ],
                        ),
                      ),
                      const SizedBox(height: 22),

                      TextFormField(
                        controller: _proofController,
                        maxLines: 2,
                        decoration: const InputDecoration(
                          labelText: 'Proof of eligibility',
                          hintText: 'Student ID number, enrollment reference, or proof details',
                          prefixIcon: Icon(Icons.fact_check_outlined),
                        ),
                        validator: (value) {
                          if (value == null || value.trim().isEmpty) {
                            return 'Proof of eligibility is required';
                          }
                          return null;
                        },
                      ),
                      const SizedBox(height: 14),

                      // Info Card
                      Container(
                        padding: const EdgeInsets.all(18),
                        decoration: BoxDecoration(
                          color: Colors.white,
                          borderRadius: BorderRadius.circular(20),
                          border: Border.all(color: BookingTheme.border),
                        ),
                        child: Row(
                          children: [
                            const Icon(
                              Icons.receipt_long_rounded,
                              color: BookingTheme.primary,
                              size: 28,
                            ),
                            const SizedBox(width: 14),
                            Expanded(
                              child: Column(
                                crossAxisAlignment: CrossAxisAlignment.start,
                                children: [
                                  Text(
                                    widget.booking.destinationTitle,
                                    style: const TextStyle(
                                      fontSize: 15,
                                      fontWeight: FontWeight.w800,
                                      color: BookingTheme.forestDark,
                                    ),
                                  ),
                                  const SizedBox(height: 4),
                                  Text(
                                    'Current Total: ${currencyFormatter.format(widget.booking.totalAmount)}',
                                    style: const TextStyle(
                                      fontSize: 13,
                                      fontWeight: FontWeight.w700,
                                      color: BookingTheme.forestDark,
                                    ),
                                  ),
                                ],
                              ),
                            ),
                          ],
                        ),
                      ),
                      const SizedBox(height: 22),

                      // Category dropdown
                      const Text(
                        'Discount Category',
                        style: TextStyle(
                          fontSize: 15,
                          fontWeight: FontWeight.w800,
                          color: BookingTheme.forestDark,
                        ),
                      ),
                      const SizedBox(height: 8),
                      Container(
                        padding: const EdgeInsets.symmetric(horizontal: 16),
                        decoration: BoxDecoration(
                          color: Colors.white,
                          borderRadius: BorderRadius.circular(14),
                          border: Border.all(color: BookingTheme.border),
                        ),
                        child: DropdownButtonHideUnderline(
                          child: DropdownButton<String>(
                            value: _selectedReasonCategory,
                            isExpanded: true,
                            icon: const Icon(Icons.arrow_drop_down_rounded),
                            items: _reasonCategories.map((cat) {
                              return DropdownMenuItem(
                                value: cat,
                                child: Text(
                                  cat,
                                  style: const TextStyle(
                                    fontSize: 14,
                                    fontWeight: FontWeight.w600,
                                    color: BookingTheme.forestDark,
                                  ),
                                ),
                              );
                            }).toList(),
                            onChanged: (val) {
                              if (val != null) {
                                setState(() => _selectedReasonCategory = val);
                              }
                            },
                          ),
                        ),
                      ),
                      // Evidence hint for the selected category
                      const SizedBox(height: 6),
                      Row(
                        children: [
                          const Icon(
                            Icons.info_outline,
                            size: 13,
                            color: BookingTheme.primary,
                          ),
                          const SizedBox(width: 6),
                          Expanded(
                            child: Text(
                              _categoryEvidenceHints[_selectedReasonCategory] ??
                                  '',
                              style: const TextStyle(
                                fontSize: 11,
                                color: BookingTheme.primary,
                                fontStyle: FontStyle.italic,
                              ),
                            ),
                          ),
                        ],
                      ),
                      const SizedBox(height: 20),

                      // Percentage input
                      const Text(
                        'Requested Discount (%)',
                        style: TextStyle(
                          fontSize: 15,
                          fontWeight: FontWeight.w800,
                          color: BookingTheme.forestDark,
                        ),
                      ),
                      const SizedBox(height: 8),
                      TextFormField(
                        controller: _percentController,
                        keyboardType: const TextInputType.numberWithOptions(
                          decimal: true,
                        ),
                        decoration: const InputDecoration(
                          hintText: 'Enter 1 to 50',
                          suffixText: '%',
                          prefixIcon: Icon(
                            Icons.percent_rounded,
                            color: BookingTheme.primary,
                          ),
                        ),
                        validator: (val) {
                          if (val == null || val.trim().isEmpty) {
                            return 'Please enter a discount percentage';
                          }
                          final numVal = double.tryParse(val.trim());
                          if (numVal == null) {
                            return 'Please enter a valid numeric percentage';
                          }
                          if (numVal <= 0 || numVal > 50) {
                            return 'Discount must be between 1% and 50%';
                          }
                          return null;
                        },
                      ),
                      const SizedBox(height: 20),

                      // Justification / Reason
                      const Text(
                        'Reason & Justification',
                        style: TextStyle(
                          fontSize: 15,
                          fontWeight: FontWeight.w800,
                          color: BookingTheme.forestDark,
                        ),
                      ),
                      const SizedBox(height: 8),
                      TextFormField(
                        controller: _reasonController,
                        maxLines: 4,
                        decoration: const InputDecoration(
                          hintText: 'Describe your reason (e.g., student ID number, group size, coupon code)...',
                        ),
                        validator: (val) {
                          if (val == null || val.trim().isEmpty) {
                            return 'Please provide a justification for this request';
                          }
                          if (val.trim().length < 10) {
                            return 'Please provide at least 10 characters of explanation';
                          }
                          return null;
                        },
                      ),
                      const SizedBox(height: 24),

                      // Escrow review note
                      Container(
                        padding: const EdgeInsets.all(14),
                        decoration: BoxDecoration(
                          color: BookingTheme.mintLight,
                          borderRadius: BorderRadius.circular(16),
                        ),
                        child: const Row(
                          children: [
                            Icon(
                              Icons.info_outline_rounded,
                              color: BookingTheme.primary,
                              size: 20,
                            ),
                            SizedBox(width: 10),
                            Expanded(
                              child: Text(
                                'Requests are audited against platform escrow policy. Approved refunds are credited directly back to your payment card.',
                                style: TextStyle(
                                  fontSize: 12,
                                  color: BookingTheme.forestDark,
                                  height: 1.3,
                                ),
                              ),
                            ),
                          ],
                        ),
                      ),
                      const SizedBox(height: 32),

                      // Submit Button
                      PrimaryGradientButton(
                        width: double.infinity,
                        height: 54,
                        isLoading: reqState.isSubmitting,
                        text: 'Submit Discount Request',
                        onPressed: reqState.isSubmitting
                            ? null
                            : _submitDiscountRequest,
                      ),
                    ],
                  ),
                ),
              ),
            ),
          );
        },
      ),
    );
  }
}
