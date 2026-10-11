import 'dart:io';
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
      String? savedLocation;

      // 1. On Android, attempt writing directly to the public Download folder
      if (Platform.isAndroid) {
        final downloadDir = Directory('/storage/emulated/0/Download');
        if (await downloadDir.exists()) {
          try {
            final targetFile = File('${downloadDir.path}/$name.pdf');
            await targetFile.writeAsBytes(pdfBytes);
            savedLocation = targetFile.path;
          } catch (e) {
            debugPrint('Direct write to /storage/emulated/0/Download failed: $e');
          }
        }
      }

      // 2. If direct write didn't succeed, prompt using system file manager (Save As)
      if (savedLocation == null || savedLocation.isEmpty) {
        savedLocation = await FileSaver.instance.saveAs(
          name: name,
          bytes: pdfBytes,
          fileExtension: 'pdf',
          mimeType: MimeType.pdf,
        );
      }

      if (context.mounted && savedLocation != null && savedLocation.isNotEmpty) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text('Saved to: $savedLocation'),
            backgroundColor: AppTheme.primaryDark,
          ),
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
