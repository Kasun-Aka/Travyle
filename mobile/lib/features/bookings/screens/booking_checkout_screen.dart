import 'dart:convert';

import 'package:flutter/foundation.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_stripe/flutter_stripe.dart';
import 'package:image_picker/image_picker.dart';
import 'package:intl/intl.dart';

import '../models/booking.dart';
import '../providers/booking_providers.dart';
import '../theme/booking_theme.dart';
import '../widgets/primary_gradient_button.dart';
import 'booking_status_screen.dart';

// Test publishable key placeholder config (never hardcoded real secret key)
const String kStripePublishableKey = String.fromEnvironment(
  'STRIPE_TEST_PUBLISHABLE_KEY',
  defaultValue: 'pk_test_51TravyleSandboxPlaceholder00000000000000',
);

class BookingCheckoutScreen extends ConsumerStatefulWidget {
  final BookingSchedule schedule;
  final DateTime selectedDate;
  final String selectedSlot;
  final int guests;

  const BookingCheckoutScreen({
    super.key,
    required this.schedule,
    required this.selectedDate,
    required this.selectedSlot,
    required this.guests,
  });

  @override
  ConsumerState<BookingCheckoutScreen> createState() =>
      _BookingCheckoutScreenState();
}

class _BookingCheckoutScreenState extends ConsumerState<BookingCheckoutScreen> {
  final TextEditingController _notesController = TextEditingController();
  final TextEditingController _receiptReferenceController =
      TextEditingController();
  final _formKey = GlobalKey<FormState>();
  final ImagePicker _imagePicker = ImagePicker();

  bool _isProcessingPayment = false;
  double _appliedDiscount = 0.0;
  BookingPaymentMethod _paymentMethod = BookingPaymentMethod.sampleCard;
  String? _receiptImageData;

  @override
  void initState() {
    super.initState();
    _initStripeSandbox();
    if (widget.guests > 6) {
      final base = widget.schedule.pricePerPerson * widget.guests;
      _appliedDiscount = base * 0.20;
    }
  }

  void _initStripeSandbox() {
    try {
      if (!kIsWeb) {
        Stripe.publishableKey = kStripePublishableKey;
      }
    } catch (_) {
      // Safe fallback if Stripe plugin isn't active on current environment
    }
  }

  @override
  void dispose() {
    _notesController.dispose();
    _receiptReferenceController.dispose();
    super.dispose();
  }

  Future<void> _pickReceiptImage(ImageSource source) async {
    final image = await _imagePicker.pickImage(
      source: source,
      imageQuality: 80,
      maxWidth: 1600,
    );
    if (image == null) return;

    final bytes = await image.readAsBytes();
    final mimeType = image.mimeType ?? 'image/jpeg';
    setState(() {
      _receiptImageData = 'data:$mimeType;base64,${base64Encode(bytes)}';
    });
  }

  Future<void> _handleCheckout({
    required double baseTrip,
    required double serviceFee,
    required double totalAmount,
  }) async {
    if (_paymentMethod != BookingPaymentMethod.sampleCard &&
        _receiptImageData == null) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('Add a photo of your payment receipt first.'),
        ),
      );
      return;
    }

    setState(() => _isProcessingPayment = true);

    try {
      // 1. If on native Android/iOS with real sandbox credentials, try PaymentSheet
      if (!kIsWeb && kStripePublishableKey.startsWith('pk_test_real')) {
        await Stripe.instance.initPaymentSheet(
          paymentSheetParameters: const SetupPaymentSheetParameters(
            merchantDisplayName: 'Travyle Escrow Services',
            paymentIntentClientSecret: 'pi_test_sandbox_secret',
            style: ThemeMode.light,
          ),
        );
        await Stripe.instance.presentPaymentSheet();
      } else {
        // 2. Mock sandbox delay for Chrome/Web and test verification
        await Future.delayed(const Duration(milliseconds: 1000));
      }

      // 3. Create booking in pending state locally as specified
      final newBooking = await ref
          .read(createBookingProvider.notifier)
          .createBooking(
            schedule: widget.schedule,
            date: widget.selectedDate,
            timeSlot: widget.selectedSlot,
            guests: widget.guests,
            basePrice: baseTrip,
            serviceFee: serviceFee,
            discountAmount: _appliedDiscount,
            totalAmount: totalAmount,
            paymentMethod: _paymentMethod,
            receiptReference: _paymentMethod == BookingPaymentMethod.sampleCard
                ? null
                : _receiptReferenceController.text.trim(),
            receiptImageData: _paymentMethod == BookingPaymentMethod.sampleCard
                ? null
                : _receiptImageData,
            notes: _notesController.text.trim().isNotEmpty
                ? _notesController.text.trim()
                : null,
          );

      if (newBooking != null && mounted) {
        if (_paymentMethod == BookingPaymentMethod.sampleCard) {
          await ref
              .read(processEscrowPaymentProvider.notifier)
              .processEscrowPayment(
                bookingId: newBooking.id,
                totalAmount: totalAmount,
                paymentMethodId: 'pm_card_visa_sandbox',
              );
        }

        if (mounted) {
          ScaffoldMessenger.of(context).showSnackBar(
            SnackBar(
              content: Text(
                _paymentMethod == BookingPaymentMethod.sampleCard
                    ? 'Sample card approved! Funds placed into escrow.'
                    : 'Receipt submitted. Your booking is pending payment verification.',
              ),
              backgroundColor: BookingTheme.primary,
            ),
          );

          Navigator.of(context).pushReplacement(
            MaterialPageRoute(
              builder: (_) => BookingStatusScreen(bookingId: newBooking.id),
            ),
          );
        }
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text('Stripe payment failed: ${e.toString()}'),
            backgroundColor: BookingTheme.errorRed,
          ),
        );
      }
    } finally {
      if (mounted) {
        setState(() => _isProcessingPayment = false);
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final traveler =
      ref.watch(currentTravelerProvider).valueOrNull ?? const <String, String>{};
    final currencyFormatter = NumberFormat.currency(
      symbol: 'LKR ',
      decimalDigits: 2,
    );

    final baseTrip = widget.schedule.pricePerPerson * widget.guests;
    final serviceFee = baseTrip * 0.05; // 5% escrow & platform fee
    final totalAmount = (baseTrip + serviceFee - _appliedDiscount).clamp(
      0.0,
      double.infinity,
    );

    return Scaffold(
      backgroundColor: BookingTheme.background,
      appBar: AppBar(
        title: const Text('Checkout & Escrow'),
        centerTitle: false,
      ),
      body: SingleChildScrollView(
        padding: const EdgeInsets.fromLTRB(20, 16, 20, 120),
        child: Form(
          key: _formKey,
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              // Tour Info Card
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
                            color: BookingTheme.mintLight,
                            borderRadius: BorderRadius.circular(12),
                          ),
                          child: const Icon(
                            Icons.travel_explore_rounded,
                            color: BookingTheme.primary,
                          ),
                        ),
                        const SizedBox(width: 12),
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
                              ),
                              Text(
                                widget.schedule.location,
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
                    const Divider(height: 24, color: BookingTheme.border),
                    Row(
                      children: [
                        _buildDetailChip(
                          icon: Icons.calendar_today_rounded,
                          text: DateFormat('EEE, d MMM y')
                              .format(widget.selectedDate),
                        ),
                        const SizedBox(width: 10),
                        _buildDetailChip(
                          icon: Icons.access_time_rounded,
                          text: widget.selectedSlot,
                        ),
                        const SizedBox(width: 10),
                        _buildDetailChip(
                          icon: Icons.people_outline_rounded,
                          text: '${widget.guests} guest(s)',
                        ),
                      ],
                    ),
                  ],
                ),
              ),
              const SizedBox(height: 20),

              // Traveler Information
              const Text(
                'Traveler Details',
                style: TextStyle(
                  fontSize: 17,
                  fontWeight: FontWeight.w800,
                  color: BookingTheme.forestDark,
                ),
              ),
              const SizedBox(height: 10),
              Container(
                padding: const EdgeInsets.all(16),
                decoration: BoxDecoration(
                  color: Colors.white,
                  borderRadius: BorderRadius.circular(18),
                  border: Border.all(color: BookingTheme.border),
                ),
                child: Column(
                  children: [
                    Row(
                      children: [
                        const Icon(
                          Icons.person_outline_rounded,
                          size: 18,
                          color: BookingTheme.textMuted,
                        ),
                        const SizedBox(width: 10),
                        Text(
                          traveler['name'] ?? 'Traveler',
                          style: const TextStyle(
                            fontSize: 14,
                            fontWeight: FontWeight.w700,
                            color: BookingTheme.forestDark,
                          ),
                        ),
                      ],
                    ),
                    const SizedBox(height: 10),
                    Row(
                      children: [
                        const Icon(
                          Icons.email_outlined,
                          size: 18,
                          color: BookingTheme.textMuted,
                        ),
                        const SizedBox(width: 10),
                        Text(
                          traveler['email'] ?? 'traveler@travyle.com',
                          style: const TextStyle(
                            fontSize: 14,
                            color: BookingTheme.forestDark,
                          ),
                        ),
                      ],
                    ),
                    const SizedBox(height: 14),
                    TextFormField(
                      controller: _notesController,
                      maxLines: 2,
                      decoration: const InputDecoration(
                        hintText: 'Special requests, dietary needs, or pickup notes...',
                        labelText: 'Booking Notes (Optional)',
                      ),
                    ),
                  ],
                ),
              ),
              const SizedBox(height: 20),

              // Payment Breakdown
              const Text(
                'Financial Breakdown',
                style: TextStyle(
                  fontSize: 17,
                  fontWeight: FontWeight.w800,
                  color: BookingTheme.forestDark,
                ),
              ),
              const SizedBox(height: 10),
              Container(
                padding: const EdgeInsets.all(18),
                decoration: BoxDecoration(
                  color: Colors.white,
                  borderRadius: BorderRadius.circular(20),
                  border: Border.all(color: BookingTheme.border),
                ),
                child: Column(
                  children: [
                    _buildCostRow(
                      'Tour Ticket (${widget.guests} x ${currencyFormatter.format(widget.schedule.pricePerPerson)})',
                      currencyFormatter.format(baseTrip),
                    ),
                    const SizedBox(height: 8),
                    _buildCostRow(
                      'Platform & Escrow Service Fee (5%)',
                      currencyFormatter.format(serviceFee),
                    ),
                    if (_appliedDiscount > 0) ...[
                      const SizedBox(height: 8),
                      _buildCostRow(
                        'Automatic group discount (20%)',
                        '- ${currencyFormatter.format(_appliedDiscount)}',
                        isDiscount: true,
                      ),
                    ],
                    const Divider(height: 24, color: BookingTheme.border),
                    Row(
                      mainAxisAlignment: MainAxisAlignment.spaceBetween,
                      children: [
                        const Text(
                          'Total (LKR)',
                          style: TextStyle(
                            fontSize: 16,
                            fontWeight: FontWeight.w800,
                            color: BookingTheme.forestDark,
                          ),
                        ),
                        Text(
                          currencyFormatter.format(totalAmount),
                          style: const TextStyle(
                            fontSize: 20,
                            fontWeight: FontWeight.w900,
                            color: BookingTheme.forestDark,
                          ),
                        ),
                      ],
                    ),
                  ],
                ),
              ),
              const SizedBox(height: 20),

              _buildPaymentMethodSection(),
              const SizedBox(height: 20),

              // Escrow Guarantee Banner
              Container(
                padding: const EdgeInsets.all(16),
                decoration: BoxDecoration(
                  color: BookingTheme.mintLight,
                  borderRadius: BorderRadius.circular(18),
                  border: Border.all(
                    color: BookingTheme.primary.withValues(alpha: 0.2),
                  ),
                ),
                child: const Row(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Icon(
                      Icons.shield_outlined,
                      color: BookingTheme.primary,
                      size: 24,
                    ),
                    SizedBox(width: 12),
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(
                            'Financial Escrow Protection Active',
                            style: TextStyle(
                              fontSize: 14,
                              fontWeight: FontWeight.w800,
                              color: BookingTheme.spruce,
                            ),
                          ),
                          SizedBox(height: 4),
                          Text(
                            'Your payment is encrypted and held securely in Stripe Escrow. Funds are only disbursed to the local guide after you complete your tour.',
                            style: TextStyle(
                              fontSize: 12,
                              color: BookingTheme.forestDark,
                              height: 1.3,
                            ),
                          ),
                        ],
                      ),
                    ),
                  ],
                ),
              ),
              const SizedBox(height: 16),

              // Post-booking discount hint
              Container(
                padding: const EdgeInsets.symmetric(
                  horizontal: 16,
                  vertical: 12,
                ),
                decoration: BoxDecoration(
                  color: const Color(0xFFFFF8E1),
                  borderRadius: BorderRadius.circular(14),
                  border: Border.all(color: const Color(0xFFFFE082)),
                ),
                child: const Row(
                  children: [
                    Icon(
                      Icons.info_outline_rounded,
                      color: Color(0xFFF59E0B),
                      size: 18,
                    ),
                    SizedBox(width: 10),
                    Expanded(
                      child: Text(
                        'Need a bigger discount? After booking, you can submit a Special Concession Request (Student ID, Senior Citizen, Financial Hardship & more).',
                        style: TextStyle(
                          fontSize: 12,
                          color: Color(0xFF78350F),
                          height: 1.35,
                        ),
                      ),
                    ),
                  ],
                ),
              ),
            ],
          ),
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
        child: PrimaryGradientButton(
          height: 56,
          isLoading: _isProcessingPayment,
          icon: const Icon(
            Icons.lock_outline_rounded,
            size: 18,
            color: Colors.white,
          ),
          text: _isProcessingPayment
              ? 'Processing...'
              : _paymentMethod == BookingPaymentMethod.sampleCard
              ? 'Pay with Sample Card (${currencyFormatter.format(totalAmount)})'
              : 'Submit Receipt (${currencyFormatter.format(totalAmount)})',
          onPressed: _isProcessingPayment
              ? null
              : () => _handleCheckout(
                  baseTrip: baseTrip,
                  serviceFee: serviceFee,
                  totalAmount: totalAmount,
                ),
        ),
      ),
    );
  }

  Widget _buildPaymentMethodSection() {
    final receiptPayment = _paymentMethod != BookingPaymentMethod.sampleCard;

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        const Text(
          'Payment Method',
          style: TextStyle(
            fontSize: 17,
            fontWeight: FontWeight.w800,
            color: BookingTheme.forestDark,
          ),
        ),
        const SizedBox(height: 10),
        Container(
          padding: const EdgeInsets.all(6),
          decoration: BoxDecoration(
            color: Colors.white,
            borderRadius: BorderRadius.circular(16),
            border: Border.all(color: BookingTheme.border),
          ),
          child: Column(
            children: [
              _buildPaymentOption(
                BookingPaymentMethod.sampleCard,
                Icons.credit_card_rounded,
                'Sample card payment',
                'Stripe test mode: no real money is charged',
              ),
              _buildPaymentOption(
                BookingPaymentMethod.bankTransferReceipt,
                Icons.account_balance_rounded,
                'Bank transfer receipt',
                'Upload your bank deposit or transfer slip',
              ),
              _buildPaymentOption(
                BookingPaymentMethod.atmCashReceipt,
                Icons.local_atm_rounded,
                'ATM cash deposit receipt',
                'Upload the receipt from your ATM cash deposit',
              ),
            ],
          ),
        ),
        if (_paymentMethod == BookingPaymentMethod.sampleCard) ...[
          const SizedBox(height: 8),
          const Text(
            'Test cards: 4242 4242 4242 4242 succeeds, 4000 0000 0000 0002 is declined.',
            style: TextStyle(fontSize: 12, color: BookingTheme.textMuted),
          ),
        ],
        if (receiptPayment) ...[
          const SizedBox(height: 10),
          TextFormField(
            controller: _receiptReferenceController,
            decoration: const InputDecoration(
              labelText: 'Receipt number or payment details',
              hintText: 'e.g. bank reference, card receipt number',
              prefixIcon: Icon(Icons.confirmation_number_outlined),
            ),
          ),
          const SizedBox(height: 10),
          _buildReceiptImagePicker(),
          const SizedBox(height: 8),
          const Text(
            'Your booking will remain pending until an administrator checks the receipt image.',
            style: TextStyle(fontSize: 12, color: BookingTheme.textMuted),
          ),
        ],
      ],
    );
  }

  Widget _buildReceiptImagePicker() {
    final imageData = _receiptImageData;
    return Container(
      width: double.infinity,
      padding: const EdgeInsets.all(12),
      decoration: BoxDecoration(
        color: BookingTheme.mintLight,
        borderRadius: BorderRadius.circular(14),
        border: Border.all(color: BookingTheme.border),
      ),
      child: imageData == null
          ? Wrap(
              alignment: WrapAlignment.spaceBetween,
              runSpacing: 8,
              children: [
                const Row(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    Icon(
                      Icons.add_a_photo_outlined,
                      color: BookingTheme.primary,
                    ),
                    SizedBox(width: 8),
                    Text(
                      'Add receipt photo',
                      style: TextStyle(fontWeight: FontWeight.w700),
                    ),
                  ],
                ),
                OutlinedButton.icon(
                  onPressed: () => _pickReceiptImage(ImageSource.camera),
                  icon: const Icon(Icons.camera_alt_outlined),
                  label: const Text('Camera'),
                ),
                OutlinedButton.icon(
                  onPressed: () => _pickReceiptImage(ImageSource.gallery),
                  icon: const Icon(Icons.photo_library_outlined),
                  label: const Text('Gallery'),
                ),
              ],
            )
          : Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                ClipRRect(
                  borderRadius: BorderRadius.circular(10),
                  child: Image.memory(
                    base64Decode(
                      imageData.substring(imageData.indexOf(',') + 1),
                    ),
                    width: double.infinity,
                    height: 180,
                    fit: BoxFit.cover,
                  ),
                ),
                Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    const Text(
                      'Receipt photo attached',
                      style: TextStyle(fontWeight: FontWeight.w700),
                    ),
                    TextButton.icon(
                      onPressed: () => setState(() => _receiptImageData = null),
                      icon: const Icon(Icons.delete_outline),
                      label: const Text('Remove'),
                    ),
                  ],
                ),
              ],
            ),
    );
  }

  Widget _buildPaymentOption(
    BookingPaymentMethod method,
    IconData icon,
    String title,
    String subtitle,
  ) {
    final selected = _paymentMethod == method;
    return RadioListTile<BookingPaymentMethod>(
      value: method,
      groupValue: _paymentMethod,
      onChanged: (value) {
        if (value != null) setState(() => _paymentMethod = value);
      },
      secondary: Icon(
        icon,
        color: selected ? BookingTheme.primary : BookingTheme.textMuted,
      ),
      title: Text(title, style: const TextStyle(fontWeight: FontWeight.w700)),
      subtitle: Text(subtitle),
      activeColor: BookingTheme.primary,
      contentPadding: const EdgeInsets.symmetric(horizontal: 4),
    );
  }

  Widget _buildDetailChip({required IconData icon, required String text}) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
      decoration: BoxDecoration(
        color: BookingTheme.background,
        borderRadius: BorderRadius.circular(8),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(icon, size: 13, color: BookingTheme.primary),
          const SizedBox(width: 4),
          Text(
            text,
            style: const TextStyle(
              fontSize: 11,
              fontWeight: FontWeight.w600,
              color: BookingTheme.forestDark,
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildCostRow(String title, String amount, {bool isDiscount = false}) {
    return Row(
      mainAxisAlignment: MainAxisAlignment.spaceBetween,
      children: [
        Text(
          title,
          style: const TextStyle(fontSize: 13, color: BookingTheme.textMuted),
        ),
        Text(
          amount,
          style: TextStyle(
            fontSize: 13,
            fontWeight: FontWeight.w700,
            color: isDiscount ? BookingTheme.primary : BookingTheme.forestDark,
          ),
        ),
      ],
    );
  }
}
