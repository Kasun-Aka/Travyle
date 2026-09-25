import 'dart:typed_data';
import 'package:flutter/material.dart';
import 'package:syncfusion_flutter_pdfviewer/pdfviewer.dart';
import '../theme/app_theme.dart';

class PdfViewerScreen extends StatelessWidget {
  final Uint8List pdfBytes;
  final String title;

  const PdfViewerScreen({
    super.key,
    required this.pdfBytes,
    this.title = 'Travel Itinerary',
  });

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: Text(title, style: const TextStyle(color: AppTheme.primaryDark, fontSize: 18)),
        backgroundColor: Colors.white,
        elevation: 1,
        iconTheme: const IconThemeData(color: AppTheme.primaryDark),
      ),
      body: SfPdfViewer.memory(
        pdfBytes,
        canShowScrollHead: false,
      ),
    );
  }
}
