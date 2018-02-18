using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using UIKit;
using CoreGraphics;
using BindingLibrary;

namespace TidyUp
{
	public class InkView : UIView
	{
		private UIBezierPath mPath;
		private List<UIBezierPath> mPathList;
		private Dictionary<char, UIBezierPath> alphabet = new Dictionary<char, UIBezierPath> ();
		private int mCurrStroke;
		private float mX, mY;
		private const float TOUCH_TOLERANCE = 2;
		private bool mMoved;
		private List<WritePadAPI.CGTracePoint> currentStroke;
		private int strokeLen = 0;
		private UIBezierPath undoneStroke;

		struct FPoint
		{
			public float X;
			public float Y;
		};
		private  FPoint _lastPoint;
		private  FPoint _previousLocation;

		private const int SEGMENT2 = 2;
		private const int SEGMENT3 = 3;

		private const float GRID_GAP = 65;
		private const int SEGMENT4 = 4;

		private const float  SEGMENT_DIST_1   = 3;
		private const float  SEGMENT_DIST_2   = 6;
		private const float  SEGMENT_DIST_3   = 12;


		private int AddPixelsXY( float X, float Y, bool bLastPoint )
		{
			float		xNew, yNew, x1, y1;
			int		nSeg = SEGMENT3;

			if ( mCurrStroke < 0 )
				return 0;

			if  ( strokeLen < 1 )  
			{
				_lastPoint.X = _previousLocation.X = X;
				_lastPoint.Y = _previousLocation.Y = Y;
				WritePadAPI.recoAddPixel(mCurrStroke, X, Y); 
				AddCurrentPoint( X, Y );
				strokeLen = 1;
				return  1;
			}

			float dx = Math.Abs( X - _lastPoint.X );
			float dy = Math.Abs( Y - _lastPoint.Y );
			if  ( (dx + dy) < SEGMENT_DIST_1 )  
			{
				_lastPoint.X = _previousLocation.X = X;
				_lastPoint.Y = _previousLocation.Y  = Y;
				WritePadAPI.recoAddPixel(mCurrStroke, X, Y); 
				AddCurrentPoint( X, Y );
				strokeLen++;
				return  1;
			}

			if ( (dx + dy) < SEGMENT_DIST_2 )  
				nSeg = SEGMENT2;
			else if ( (dx + dy) < SEGMENT_DIST_3 )
				nSeg = SEGMENT3;
			else
				nSeg = SEGMENT4;
			int     nPoints = 0;
			for ( int i = 1; i < nSeg;  i++ )  
			{
				x1 = _previousLocation.X + ((X - _previousLocation.X)*i ) / nSeg;  //the point "to look at"
				y1 = _previousLocation.Y + ((Y - _previousLocation.Y)*i ) / nSeg;  //the point "to look at"

				xNew = _lastPoint.X + (x1 - _lastPoint.X) / nSeg;
				yNew = _lastPoint.Y + (y1 - _lastPoint.Y) / nSeg;

				if ( xNew != _lastPoint.X || yNew != _lastPoint.Y )
				{
					_lastPoint.X = xNew;
					_lastPoint.Y = yNew;
					WritePadAPI.recoAddPixel(mCurrStroke, xNew, yNew); 
					AddCurrentPoint( xNew, yNew );
					strokeLen++;
					nPoints++;
				}
			}

			if ( bLastPoint )  
			{
				// add last point
				if ( X != _lastPoint.X || Y != _lastPoint.Y )  
				{
					_lastPoint.X = X;
					_lastPoint.Y = Y;
					WritePadAPI.recoAddPixel(mCurrStroke, X, Y); 
					AddCurrentPoint( X, Y );
					strokeLen++;
					nPoints++;
				}
			}

			_previousLocation.X = X;
			_previousLocation.Y = Y;
			return nPoints;
		}


		public InkView ()
		{
			BackgroundColor = UIColor.White;
			mPath = new UIBezierPath();
			mPathList = new List<UIBezierPath>();
			mPath.LineWidth = WritePadAPI.DEFAULT_INK_WIDTH;
			mPath.LineCapStyle = CGLineCap.Round;
			mPath.LineJoinStyle = CGLineJoin.Round;
			mCurrStroke = -1;
		}

		public override void Draw(CGRect rect)
		{
			base.Draw (rect); 
			using (CGContext g = UIGraphics.GetCurrentContext ()) 
			{
				// draw grid
				UIBezierPath path = new UIBezierPath();
				path.LineWidth = 0.1f;
				UIColor.LightGray.SetStroke ();
				for ( float y = GRID_GAP; y < rect.Height; y += GRID_GAP )
				{
					path.MoveTo( new CGPoint( 0, y ) );
					path.AddLineTo( new CGPoint( rect.Width, y ) );
					path.Stroke();
				}

				UIColor.Blue.SetStroke ();
				foreach(var aMPathList in mPathList) 
				{
					aMPathList.Stroke ();
				}
				mPath.Stroke ();
			}
		}

		private void AddCurrentPoint(float mX, float mY)
		{
			var point = new WritePadAPI.CGTracePoint ();
			point.pressure = WritePadAPI.DEFAULT_INK_PRESSURE;
			point.pt = new WritePadAPI.CGPoint ();
			point.pt.x = mX;
			point.pt.y = mY;
			currentStroke.Add (point);
		}

		public override void TouchesBegan (Foundation.NSSet touches, UIEvent evt)
		{
			base.TouchesBegan (touches, evt);
			currentStroke = new List<WritePadAPI.CGTracePoint> ();
			UITouch touch = (UITouch)touches.AnyObject;
			var location = touch.LocationInView (this);
			mPath.RemoveAllPoints ();
			mPath.MoveTo(new CGPoint(location.X, location.Y));
			mX = (float)location.X;
			mY = (float)location.Y;
			AddCurrentPoint (mX, mY);
			mMoved = false;
			strokeLen = 0;
			mCurrStroke = WritePadAPI.recoNewStroke( WritePadAPI.DEFAULT_INK_WIDTH, 0xFF0000FF);   
			AddPixelsXY( mX, mY, false );
		}

		public override void TouchesMoved (Foundation.NSSet touches, UIEvent evt)
		{
			base.TouchesMoved (touches, evt);
			UITouch touch = (UITouch)touches.AnyObject;
			var location = touch.LocationInView (this);

			float dx = (float)Math.Abs(location.X - mX);
			float dy = (float)Math.Abs(location.Y - mY);
			if (dx >= TOUCH_TOLERANCE || dy >= TOUCH_TOLERANCE)
			{
				mPath.AddQuadCurveToPoint(new CGPoint((location.X + mX) / 2f, (location.Y + mY) / 2f), new CGPoint(mX, mY));
				mMoved = true;
				mX = (float)location.X;
				mY = (float)location.Y;
				AddPixelsXY( mX, mY, false );
			}
			this.SetNeedsDisplay ();
		}

		public override void TouchesCancelled (Foundation.NSSet touches, UIEvent evt)
		{
			base.TouchesCancelled (touches, evt);
		}

		public override void TouchesEnded (Foundation.NSSet touches, UIEvent evt)
		{
			base.TouchesEnded (touches, evt);
			var gesture = WritePadAPI.detectGesture (WritePadAPI.GEST_CUT | WritePadAPI.GEST_RETURN | WritePadAPI.GEST_UNDO | WritePadAPI.GEST_REDO, currentStroke);
			if (!mMoved)
				mX++;
			AddPixelsXY( mX, mY, true );

			mCurrStroke = -1;
			mMoved = false;
			strokeLen = 0;

			mPath.AddLineTo(new CGPoint(mX, mY));
			mPathList.Add(mPath);
			mPath = new UIBezierPath ();
			mPath.LineWidth = WritePadAPI.DEFAULT_INK_WIDTH;
			mPath.LineCapStyle = CGLineCap.Round;
			mPath.LineJoinStyle = CGLineJoin.Round;

			SetNeedsDisplay ();

			switch (gesture)
			{
				case WritePadAPI.GEST_RETURN:
					if (OnReturnGesture != null)
					{
						mPathList.RemoveAt(mPathList.Count - 1);
						WritePadAPI.recoDeleteLastStroke();
						SetNeedsDisplay ();
						OnReturnGesture();                    
					}
					break;
				case WritePadAPI.GEST_CUT:
					if (OnCutGesture != null)
					{
						mPathList.RemoveAt(mPathList.Count - 1);
						WritePadAPI.recoDeleteLastStroke();
						SetNeedsDisplay ();
						OnCutGesture();                    
					}
					break;
				case WritePadAPI.GEST_UNDO:
					if (OnUndoGesture != null)
					{
						undoneStroke = mPathList [mPathList.Count - 1];
						mPathList.RemoveAt (mPathList.Count - 1);
						WritePadAPI.recoDeleteLastStroke ();
						SetNeedsDisplay ();
						OnUndoGesture ();
					}
					break;
				case WritePadAPI.GEST_REDO:
					if (OnRedoGesture != null && undoneStroke != null)
					{
						mPathList.Add (undoneStroke);
						UIColor.Blue.SetStroke ();
						foreach (var aMPathList in mPathList)
						{
							aMPathList.Stroke ();
						}
						mPath.Stroke ();
						SetNeedsDisplay ();
						OnRedoGesture ();
					}
					break;
			}
		}


		public struct WordAlternative
		{
			public string Word;
			public int Weight;
		}


		public string Recognize (bool bLearn)
		{
			var wordList = new List<List<WordAlternative>> ();
			var count = WritePadAPI.recoStrokeCount ();
			string defaultResult = WritePadAPI.recoInkData (count, false, false, false, false);

			/*if (defaultResult.Length > 0 && !alphabet.ContainsKey (defaultResult[0]))
			{
				alphabet.Add (defaultResult[0], mPathList [mPathList.Count - 1]);
			}*/
				
			var wordCount = WritePadAPI.recoResultColumnCount ();
			for (var i = 0; i < wordCount; i++)
			{
				var wordAlternativesList = new List<WordAlternative> ();
				var altCount = WritePadAPI.recoResultRowCount (i);
				for (var j = 0; j < altCount; j++)
				{
					String word = WritePadAPI.recoResultWord (i, j);
					if (word == "<--->")
						word = "*Error*";
					if (string.IsNullOrEmpty (word))
						continue;
					uint flags = WritePadAPI.recoGetFlags ();
					var weight = WritePadAPI.recoResultWeight (i, j);
					if (j == 0 && bLearn && weight > 75 && 0 != (flags & WritePadAPI.FLAG_ANALYZER))
					{
						WritePadAPI.recoLearnWord (word, weight);
					}

					if (wordAlternativesList.All (x => x.Word != word))
					{
						wordAlternativesList.Add (new WordAlternative {
							Word = word,
							Weight = weight
						});
					}
				}
			}
			return defaultResult;
		}
			
		public void cleanView(bool emptyAll)
		{
			WritePadAPI.recoResetInk();
			mCurrStroke = -1;
			mPathList.Clear();
			mPath.RemoveAllPoints ();      
			SetNeedsDisplay ();
		}

		public int getNumPaths()
		{
			return mPathList.Count;
		}

		public void writeText(String input, int width, int height)
		{
			int charsWritten = 0;

			foreach (char symbol in input)
			{
				if (alphabet.ContainsKey (symbol))
				{
					using (CGContext g = UIGraphics.GetCurrentContext ())
					{
						UIColor.Blue.SetStroke ();
						alphabet [symbol].Stroke ();
					}
				}
			}
		}

		public event Action OnReturnGesture;
		public event Action OnCutGesture;
		public event Action OnUndoGesture;
		public event Action OnRedoGesture;
	}
}
