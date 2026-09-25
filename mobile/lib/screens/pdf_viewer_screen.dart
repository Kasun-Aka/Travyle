import 'dart:typed_data';
import 'package:flutter/material.dart';
import 'package:syncfusion_flutter_pdfviewer/pdfviewer.dart';
import 'package:file_saver/file_saver.dart';
import '../theme/app_theme.dart';

class PdfViewerScreen extends StatelessWidget {
  final Uint8List pdfBytes;
  final String title;

  const PdfViewerScreen({
    super.key,
    required this.pdfBytes,
    this.title = 'Travel Itinerary',
  });

  Future<void> _saveFile(BuildContext context) async {
    try {
      final name = 'Travel_Pass_${DateTime.now().millisecondsSinceEpoch}';
      
      final result = await FileSaver.instance.saveFile(
        name: name,
        bytes: pdfBytes,
        fileExtension: 'pdf',
        mimeType: MimeType.pdf,
      );

      if (context.mounted && result.isNotEmpty) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Saved to: $result')),
        );
      }
    } catch (e) {
      if (context.mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Failed to save file: $e')),
        );
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: Text(title, style: const TextStyle(color: AppTheme.primaryDark, fontSize: 18)),
        backgroundColor: Colors.white,
        elevation: 1,
        iconTheme: const IconThemeData(color: AppTheme.primaryDark),
        actions: [
          IconButton(
            icon: const Icon(Icons.download),
            tooltip: 'Download to Device',
            onPressed: () => _saveFile(context),
          ),
        ],
      ),
      body: SfPdfViewer.memory(
        pdfBytes,
        canShowScrollHead: false,
      ),
    );
  }
}
