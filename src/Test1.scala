/**
 * Created with IntelliJ IDEA.
 * User: vans
 * Date: 31/08/13
 * Time: 10:06
 * To change this template use File | Settings | File Templates.
 */
object Test  {

  def isPos(x : Int) = if (x > 0) true else false

  def countDown(n : Int) {
    var x = n
    for (i <- n to 0 by -1) println(i)
  }

  def unicodeProduct(str : String) : Int = str.foldLeft(1)(_ * _)

  def unicodeRecProduct(str : String) : Int = if (str.length > 0) str.head * unicodeRecProduct(str.tail) else 1

  def main(args:Array[String]){

    println(unicodeProduct("Hello"))
    println(unicodeRecProduct("Hello"))
  }
}
//for (int i = 10; i >= 0; i--) System.out.println(i);
