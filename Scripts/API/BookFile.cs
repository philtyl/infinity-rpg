﻿// Project:         Daggerfall Tools For Unity
// Copyright:       Copyright (C) 2009-2015 Daggerfall Workshop
// Web Site:        http://www.dfworkshop.net
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Source Code:     https://github.com/Interkarma/daggerfall-unity
// Original Author: Gavin Clayton (interkarma@dfworkshop.net)
// Contributors:    
// 
// Notes:
//

#region Using Statements
using System;
using System.Text;
using System.Collections.Generic;
using System.IO;
using DaggerfallConnect.Utility;
#endregion

namespace DaggerfallConnect.Arena2
{
    /// <summary>
    /// Reads a Daggerfall book file.
    /// </summary>
    public class BookFile
    {
        #region Fields

        const string books = "books";
        const string naughty = "naughty";

        FileProxy bookFile = new FileProxy();
        BookHeader header = new BookHeader();

        #endregion

        #region Properties

        public string Title { get { return header.Title; } }
        public string Author { get { return header.Author; } }
        public bool IsNaughty { get { return header.IsNaughty; } }
        public int Price { get { return (int)header.Price; } }
        public int PageCount { get { return header.PageCount; } }

        #endregion

        #region Structs & Enums

        /// <summary>
        /// Defines a book file header.
        /// </summary>
        public struct BookHeader
        {
            public string Title;                    // Title of book
            public string Author;                   // Internal name of book author
            public bool IsNaughty;                  // True if book has adult content
            public Byte[] NullValues;               // Null-value bytes
            public UInt32 Price;                    // Influences book price
            public UInt16 Unknown1;
            public UInt16 Unknown2;
            public UInt16 Unknown3;
            public UInt16 PageCount;                // Number of pages
            public UInt32[] PageOffsets;            // Array of page offsets, PageCount elements long
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Opens the specified book file.
        /// </summary>
        /// <param name="arena2">Arena2 path.</param>
        /// <param name="name">Name of book to open.</param>
        public bool OpenBook(string arena2, string name, FileUsage fileUsage = FileUsage.UseMemory, bool readOnly = true)
        {
            // Validate filename
            if (!name.StartsWith("BOK", StringComparison.InvariantCultureIgnoreCase) ||
                !name.EndsWith(".TXT", StringComparison.InvariantCultureIgnoreCase))
            {
                return false;
            }

            // Load book file
            string booksPath = Path.Combine(arena2, books);
            if (!bookFile.Load(Path.Combine(booksPath, name), fileUsage, readOnly))
                return false;

            // Read book
            ReadHeader();

            return true;
        }

        /// <summary>
        /// Reads the TextResource tokens for this page record.
        /// </summary>
        /// <param name="page">Page index.</param>
        /// <returns>TextResource token array.</returns>
        public TextFile.Token[] GetPageTokens(int page)
        {
            if (page < 0 || page >= PageCount)
                throw new IndexOutOfRangeException("BookFile: Invalid page index.");

            return ReadTokens((int)header.PageOffsets[page]);
        }

        #endregion

        #region Private Methods

        void ReadHeader()
        {
            BinaryReader reader = bookFile.GetReader();

            header = new BookHeader();
            header.Title = bookFile.ReadCStringSkip(reader, 0, 64);
            header.Author = bookFile.ReadCStringSkip(reader, 0, 64);
            header.IsNaughty = (bookFile.ReadCStringSkip(reader, 0, 8) == naughty);
            header.NullValues = reader.ReadBytes(88);
            header.Price = reader.ReadUInt32();
            header.Unknown1 = reader.ReadUInt16();
            header.Unknown2 = reader.ReadUInt16();
            header.Unknown3 = reader.ReadUInt16();
            header.PageCount = reader.ReadUInt16();
            header.PageOffsets = new UInt32[header.PageCount];
            for (int i = 0; i < header.PageCount; i++)
            {
                header.PageOffsets[i] = reader.ReadUInt32();
            }
        }

        TextFile.Token[] ReadTokens(int offset, byte terminatorByte = 0xf6)
        {
            List<TextFile.Token> tokens = new List<TextFile.Token>();

            byte[] buffer = bookFile.GetBytes();
            int position = offset;
            while (position < buffer.Length)
            {
                byte nextByte = buffer[position];
                if (nextByte == terminatorByte)
                    break;

                if (nextByte >= (byte)TextFile.Formatting.FirstCharacter && nextByte <= (byte)TextFile.Formatting.LastCharacter)
                    tokens.Add(ReadTextToken(ref buffer, position, out position));
                else
                    tokens.Add(ReadFormattingToken(ref buffer, position, out position));
            }

            return tokens.ToArray();
        }

        TextFile.Token ReadTextToken(ref byte[] buffer, int position, out int endPosition)
        {
            // Find length of text data
            int start = position;
            int count = 0;
            while (position < buffer.Length)
            {
                byte nextByte = buffer[position++];
                if (nextByte >= (byte)TextFile.Formatting.FirstCharacter && nextByte <= (byte)TextFile.Formatting.LastCharacter)
                    count++;
                else
                    break;
            }

            // Create token
            TextFile.Token token = new TextFile.Token();
            token.formatting = TextFile.Formatting.Text;
            token.text = Encoding.UTF8.GetString(buffer, start, count);
            endPosition = start + count;

            return token;
        }

        TextFile.Token ReadFormattingToken(ref byte[] buffer, int position, out int endPosition)
        {
            TextFile.Formatting formatting = (TextFile.Formatting)buffer[position++];

            int x = 0, y = 0;
            TextFile.Token token = new TextFile.Token();
            token.formatting = formatting;
            switch (token.formatting)
            {
                case TextFile.Formatting.NewLineOffset:
                    break;
                case TextFile.Formatting.FontPrefix:
                    x = buffer[position++];
                    break;
            }
            token.x = x;
            token.y = y;
            endPosition = position;

            return token;
        }

        #endregion
    }
}