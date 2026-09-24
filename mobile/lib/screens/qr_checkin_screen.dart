import 'package:flutter/material.dart';
import 'package:mobile_scanner/mobile_scanner.dart';

class QRCheckinScreen extends StatefulWidget {
  const QRCheckinScreen({super.key});

  @override
  State<QRCheckinScreen> createState() => _QRCheckinScreenState();
}

class _QRCheckinScreenState extends State<QRCheckinScreen> {
  final MobileScannerController _scannerController = MobileScannerController();

  @override
  void dispose() {
    _scannerController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: const Color(0xFFF8F9FA),
      appBar: AppBar(
        backgroundColor: Colors.transparent,
        elevation: 0,
        leading: Padding(
          padding: const EdgeInsets.only(left: 20.0),
          child: InkWell(
            onTap: () => Navigator.pop(context),
            borderRadius: BorderRadius.circular(24.0),
            child: Container(
              decoration: BoxDecoration(
                shape: BoxShape.circle,
                border: Border.all(color: Colors.grey.shade300),
                color: Colors.white,
              ),
              child: const Icon(Icons.arrow_back_ios_new,
                  color: Color(0xFF133E4D), size: 16),
            ),
          ),
        ),
        title: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: const [
            Text(
              'QR Tour Check-In',
              style: TextStyle(
                color: Color(0xFF133E4D),
                fontSize: 20.0,
                fontWeight: FontWeight.bold,
              ),
            ),
            Text(
              'Verify traveler tour pass',
              style: TextStyle(
                color: Colors.grey,
                fontSize: 13.0,
              ),
            ),
          ],
        ),
      ),
      body: SafeArea(
        child: Padding(
          padding: const EdgeInsets.all(20.0),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Expanded(
                flex: 3,
                child: Container(
                  width: double.infinity,
                  decoration: BoxDecoration(
                    color: const Color(0xFF133E4D),
                    borderRadius: BorderRadius.circular(20.0),
                  ),
                  child: ClipRRect(
                    borderRadius: BorderRadius.circular(20.0),
                    child: Stack(
                      children: [
                        MobileScanner(
                          controller: _scannerController,
                          onDetect: (capture) {
                            final List<Barcode> barcodes = capture.barcodes;
                            if (barcodes.isNotEmpty) {
                              // Handle QR code read
                              debugPrint('QR Code Found: ${barcodes.first.rawValue}');
                            }
                          },
                        ),
                        // Scanner Overlay Styling
                        Center(
                          child: Container(
                            width: 200,
                            height: 200,
                            decoration: BoxDecoration(
                              border: Border.all(
                                color: Colors.white.withOpacity(0.5),
                                width: 2.0,
                              ),
                              borderRadius: BorderRadius.circular(24.0),
                            ),
                          ),
                        ),
                        // Top badge
                        Positioned(
                          top: 16,
                          left: 16,
                          child: Container(
                            padding: const EdgeInsets.symmetric(
                                horizontal: 12.0, vertical: 8.0),
                            decoration: BoxDecoration(
                              color: Colors.white,
                              borderRadius: BorderRadius.circular(20.0),
                            ),
                            child: Row(
                              mainAxisSize: MainAxisSize.min,
                              children: const [
                                CircleAvatar(
                                    radius: 4,
                                    backgroundColor: Color(0xFF1FA88A)),
                                SizedBox(width: 8),
                                Text(
                                  'SCANNING FOR QR...',
                                  style: TextStyle(
                                    fontSize: 10.0,
                                    fontWeight: FontWeight.w800,
                                    color: Color(0xFF133E4D),
                                    letterSpacing: 0.5,
                                  ),
                                ),
                              ],
                            ),
                          ),
                        ),
                      ],
                    ),
                  ),
                ),
              ),
              const SizedBox(height: 32.0),
              const Text(
                'HOW IT WORKS',
                style: TextStyle(
                  fontSize: 14.0,
                  fontWeight: FontWeight.w800,
                  color: Color(0xFF133E4D),
                  letterSpacing: 0.5,
                ),
              ),
              const SizedBox(height: 12.0),
              Text(
                'POINT YOUR CAMERA AT THE TRAVELER\'S QR PASS.\nCHECK-IN CONFIRMS AUTOMATICALLY ONCE IT\'S SCANNED.',
                style: TextStyle(
                  fontSize: 12.0,
                  color: Colors.blueGrey.shade400,
                  height: 1.5,
                  letterSpacing: 0.5,
                ),
              ),
              const Spacer(),
            ],
          ),
        ),
      ),
    );
  }
}
